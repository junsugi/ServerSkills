using System.Diagnostics;
using Google.Protobuf;
using Google.Protobuf.Protocol;
using ServerSkills.Job;
using ServerSkills.Monitoring;

namespace ServerSkills.Game.Room;

public enum PickItemMode
{
    Unsafe,
    Lock,
    Claim,
}

public class GameRoom(
    int roomId,
    PickItemMode mode,
    IPickItemStrategy pickItemStrategy,
    PickItemMetrics pickMetrics,
    MoveMetrics moveMetrics) : JobSerializer
{
    public int RoomId { get; set; } = roomId;
    public int PlayerCount => _players.Count;
    public PickItemMetrics PickMetrics { get; } = pickMetrics;
    public MoveMetrics MoveMetrics { get; } = moveMetrics;

    private Dictionary<int, Player> _players = new();
    private Dictionary<int, RoomItem> _roomItems = new();
    
    private readonly Dictionary<(int playerId, int requestId), PickItemRequestRecord> _pickItemRequests = new();
    private object _lock = new();
    
    private static readonly TimeSpan PickItemRequestTtl = TimeSpan.FromMinutes(1);
    private const int MaxPickItemRequestCache = 256;
    
    public void Move(Player player, int requestId)
    {
        Push(() =>
        {
            player.Position.X = Math.Clamp(player.Position.X + 0.5f, 0f, 101f);
            player.Position.Y = Math.Clamp(player.Position.Y + 0.5f, 0f, 101f);

            S_Move movePacket = new S_Move();
            movePacket.RequestId = requestId;
            movePacket.ResultCode = ResultCode.Success;
            movePacket.Player = PlayerMapper.ToDto(player);
            player.Session.Send(movePacket);

            int count = BroadCastAoi(movePacket, player);
            MoveMetrics.RecordMove(count);
        });
    }
    
    private int BroadCast(IMessage packet, Player? exceptPlayer)
    {
        int sendCount = 0;

        foreach (Player player in _players.Values)
        {
            if (player == exceptPlayer)
                continue;
            player.Session.Send(packet);
            sendCount++;
        }

        return sendCount;
    }

    private const float AoiRange = 3f;

    private bool IsInAoi(Player center, Player target)
    {
        float dx = Math.Abs(center.Position.X - target.Position.X);
        float dy = Math.Abs(center.Position.Y - target.Position.Y);

        return dx <= AoiRange && dy <= AoiRange;
    }

    private int BroadCastAoi(IMessage packet, Player centerPlayer)
    {
        int sendCount = 0;

        foreach (Player player in _players.Values)
        {
            if (!IsInAoi(centerPlayer, player))
                continue;

            if (player == centerPlayer) 
                continue;
            
            player.Session.Send(packet);
            sendCount++;
        }

        return sendCount;
    }


    public void EnterGame(int requestId, GameObject? gameObject, Action<int, JobMetrics> onCompleted)
    {
        long enqueueAt = Stopwatch.GetTimestamp();

        Push(() =>
        {
            Thread.Sleep(100);
            long startAt = Stopwatch.GetTimestamp();

            if (gameObject == null)
                Console.WriteLine($"GameObject is Null..");

            GameObjectType type = ObjectManager.Instance.GetObjectTypeById(gameObject!.ObjectId);
            if (type == GameObjectType.PLAYER)
            {
                Player player = (Player)gameObject;
                player.GameRoom = this;
                _players.Add(player.ObjectId, player);

                player.Session.SetPlayer(player);
                player.Session.MarkEnterGame();
                player.Session.SendEnterGame(requestId, ResultCode.Success, player);

                SpawnPlayer(player);

                long endAt = Stopwatch.GetTimestamp();

                onCompleted.Invoke(RoomId, new JobMetrics(enqueueAt, startAt, endAt));
            }

            // 나중에 아이템도 EnterGame에서 스폰처리 가능.
        });
    }

    private void SpawnPlayer(Player player)
    {
        S_Spawn packet = new S_Spawn();
        SpawnObjectInfo spawnObject = new SpawnObjectInfo();
        spawnObject.Player = PlayerMapper.ToDto(player);
        packet.SpawnObject = spawnObject;
        BroadCast(packet, player);
    }

    public void SpawnItem(Item item)
    {
        Push(() =>
        {
            S_Spawn packet = new S_Spawn();

            RoomItem roomItem = new RoomItem(item);

            if (!_roomItems.TryAdd(roomItem.ObjectId, roomItem))
            {
                Console.WriteLine("Add item failed");
                packet.ResultCode = ResultCode.InternalError;
                BroadCast(packet, null);
                return;
            }

            SpawnObjectInfo spawnObject = new SpawnObjectInfo();
            spawnObject.Item = ItemMapper.ToDto(item);
            packet.SpawnObject = spawnObject;
            BroadCast(packet, null);
        });
    }

    public void PickItem(Player player, int requestId, int objectId)
    {
        if (TryReplayPickItem(player, requestId, objectId))
            return;

        if (!TryBeginPickItem(player.ObjectId, requestId, objectId))
            return;
        
        switch (mode)
        {
            case PickItemMode.Unsafe:
            case PickItemMode.Lock:
                pickItemStrategy.Pick(this, player, requestId, objectId);
                break;
            case PickItemMode.Claim:
                Push(() => { pickItemStrategy.Pick(this, player, requestId, objectId); });
                break;
        }
    }
    
    private bool TryReplayPickItem(Player player, int requestId, int objectId)
    {
        PickItemRequestRecord? record;

        lock (_lock)
        {
            PruneExpiredPickItemRequests();

            if (!_pickItemRequests.TryGetValue((player.ObjectId, requestId), out record))
                return false;

            if (record.ObjectId != objectId)
            {
                LogPickItemIdempotency(
                    "PICK_IDEMPOTENCY_CONFLICT",
                    player.ObjectId,
                    requestId,
                    objectId,
                    $"originalItemId={record.ObjectId}");
                
                record = new PickItemRequestRecord()
                {
                    ObjectId = objectId,
                    State = PickItemRequestState.Completed,
                    ResultCode = ResultCode.InvalidRequest,
                    ExpiresAt = DateTimeOffset.UtcNow.Add(PickItemRequestTtl),
                };
            } 
            else if (record.State == PickItemRequestState.Pending)
            {
                LogPickItemIdempotency(
                    "PICK_IDEMPOTENCY_PENDING",
                    player.ObjectId,
                    requestId,
                    objectId);
                return true;
            }
        }
        
        LogPickItemIdempotency(
            "PICK_IDEMPOTENCY_REPLAY",
            player.ObjectId,
            requestId,
            objectId,
            $"resultCode={record.ResultCode}");
        
        S_PickItem packet = new S_PickItem
        {
            RequestId = requestId,
            ResultCode = record.ResultCode,
            ItemInfo = record.ItemInfo?.Clone(),
        };

        player.Session.Send(packet);
        return true;
    }
    
    private bool TryBeginPickItem(int playerId, int requestId, int objectId)
    {
        lock (_lock)
        {
            PruneExpiredPickItemRequests();

            if (_pickItemRequests.ContainsKey((playerId, requestId)))
                return false;

            _pickItemRequests[(playerId, requestId)] = new PickItemRequestRecord
            {
                ObjectId = objectId,
                State = PickItemRequestState.Pending,
                ExpiresAt = DateTimeOffset.UtcNow.Add(PickItemRequestTtl),
            };
            
            LogPickItemIdempotency("PICK_IDEMPOTENCY_BEGIN", playerId, requestId, objectId);

            return true;
        }
    }
    
    internal void CompletePickItemRequest(
        Player player,
        int requestId,
        int objectId,
        ResultCode resultCode,
        Item? item = null)
    {
        ItemInfo? itemInfo = item == null ? null : ItemMapper.ToDto(item);
        
        lock (_lock)
        {
            _pickItemRequests[(player.ObjectId, requestId)] = new PickItemRequestRecord
            {
                ObjectId = objectId,
                State = PickItemRequestState.Completed,
                ResultCode = resultCode,
                ItemInfo = itemInfo?.Clone(),
                ExpiresAt = DateTimeOffset.UtcNow.Add(PickItemRequestTtl),
            };

            LogPickItemIdempotency(
                "PICK_IDEMPOTENCY_COMPLETE",
                player.ObjectId,
                requestId,
                objectId,
                $"resultCode={resultCode}");
            
            PruneExpiredPickItemRequests();
        }
        
        S_PickItem packet = new S_PickItem
        {
            RequestId = requestId,
            ResultCode = resultCode,
            ItemInfo = itemInfo?.Clone(),
        };

        player.Session.Send(packet);
    }
    
    private void PruneExpiredPickItemRequests()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;

        foreach (var id in _pickItemRequests
                     .Where(pair => pair.Value.ExpiresAt <= now)
                     .Select(pair => pair.Key)
                     .ToList())
        {
            _pickItemRequests.Remove(id);
        }

        if (_pickItemRequests.Count <= MaxPickItemRequestCache)
            return;

        foreach (var id in _pickItemRequests
                     .OrderBy(pair => pair.Value.ExpiresAt)
                     .Take(_pickItemRequests.Count - MaxPickItemRequestCache)
                     .Select(pair => pair.Key)
                     .ToList())
        {
            _pickItemRequests.Remove(id);
        }
    }
    
    public void LeaveGame(Player leavePlayer)
    {
        Push(() =>
        {
            _players.Remove(leavePlayer.ObjectId);
            leavePlayer.GameRoom = null;
            leavePlayer.Session = null;
        });
    }

    private void LogPickItemIdempotency(
        string eventName,
        int playerId,
        int requestId,
        int objectId,
        string? detail = null)
    {
        
        Console.WriteLine(
            $"[{DateTimeOffset.Now:HH:mm:ss.fff}][{eventName}] " +
            $"roomId={RoomId}, playerId={playerId}, itemId={objectId}, requestId={requestId}" +
            (detail is null ? "" : $", {detail}")
        );
    }
    
    internal bool HasRoomItem(int objectId) => _roomItems.ContainsKey(objectId);
    internal RoomItem? GetRoomItem(int objectId) => _roomItems.GetValueOrDefault(objectId);
    internal bool RemoveRoomItem(int objectId) => _roomItems.Remove(objectId);
}