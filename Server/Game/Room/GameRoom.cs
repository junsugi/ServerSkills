using System.Diagnostics;
using Google.Protobuf;
using Google.Protobuf.Protocol;
using ServerSkills.Job;
using ServerSkills.Monitoring;

namespace ServerSkills.Game.Room;

public class GameRoom(int roomId, IPickItemStrategy pickItemStrategy, PickItemMetrics pickMetrics, MoveMetrics moveMetrics) : JobSerializer
{
    public int RoomId { get; set; } = roomId;
    public int PlayerCount => _players.Count;
    public PickItemMetrics PickMetrics { get; } = pickMetrics;
    public MoveMetrics MoveMetrics { get; } = moveMetrics;

    private Dictionary<int, Player> _players = new();
    private Dictionary<int, RoomItem> _roomItems = new();

    public int BroadCast(IMessage packet, Player? exceptPlayer)
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

    public void PickItemUnsafe(Player player, int requestId, int objectId)
    {
        pickItemStrategy.Pick(this, player, requestId, objectId);
    }

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
            int count = BroadCast(movePacket, null);
            MoveMetrics.RecordMove(count);
        });
    }

    internal bool HasRoomItem(int objectId) => _roomItems.ContainsKey(objectId);
    internal RoomItem? GetRoomItem(int objectId) => _roomItems.GetValueOrDefault(objectId);
    internal bool RemoveRoomItem(int objectId) => _roomItems.Remove(objectId);
}