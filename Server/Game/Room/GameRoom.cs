using System.Diagnostics;
using Google.Protobuf;
using Google.Protobuf.Protocol;
using Microsoft.VisualBasic;
using ServerSkills.Job;
using ServerSkills.Monitoring;

namespace ServerSkills.Game.Room;

public class GameRoom(int roomId, IPickItemStrategy pickItemStrategy, PickItemMetrics metrics) : JobSerializer
{
    public int RoomId {get; set;} = roomId;
    public int PlayerCount => _players.Count;
    public PickItemMetrics Metrics { get; } = metrics;

    private Dictionary<int, Player> _players = new();
    private Dictionary<int, RoomItem> _roomItems = new();
    
    public void BroadCast(IMessage packet)
    {
        foreach (Player player in _players.Values)
            player.Session.Send(packet);
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
                
                long endAt = Stopwatch.GetTimestamp();
                
                onCompleted.Invoke(RoomId, new JobMetrics(enqueueAt, startAt, endAt));
            }
        });
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
                BroadCast(packet);
                return;
            }

            SpawnObjectInfo spawnObject = new SpawnObjectInfo();
            spawnObject.Item = ItemMapper.ToDto(item);
            packet.SpawnObject = spawnObject;
            BroadCast(packet);
        });
    }

    public void PickItemUnsafe(Player player, int requestId, int objectId)
    {
        pickItemStrategy.Pick(this, player, requestId, objectId);
    }

    internal bool HasRoomItem(int objectId) => _roomItems.ContainsKey(objectId);
    internal RoomItem? GetRoomItem(int objectId) => _roomItems.GetValueOrDefault(objectId);
    internal bool RemoveRoomItem(int objectId) => _roomItems.Remove(objectId);
}