using System.Diagnostics;
using Google.Protobuf;
using Google.Protobuf.Protocol;
using Microsoft.VisualBasic;
using ServerSkills.Job;

namespace ServerSkills.Game.Room;

public class GameRoom(int roomId) : JobSerializer
{
    public int RoomId {get; set;} = roomId;
    public int PlayerCount => _players.Count;

    private Dictionary<int, Player> _players = new();
    private Dictionary<int, Item> _items = new();
    
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
        
            if (!_items.TryAdd(item.ObjectId, item))
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
        if (!_items.ContainsKey(objectId))
        {
            Console.WriteLine($"[Pick Failed] Room={RoomId}, Player={player.ObjectId}, Item={objectId}");
            S_PickItem pickItemPacket = new S_PickItem();
            pickItemPacket.RequestId = requestId;
            pickItemPacket.ResultCode = ResultCode.InvalidRequest;
            player.Session.SendPickItem(requestId, ResultCode.InvalidRequest);
            return;
        }
        
        Thread.Sleep(100); // 경합 상황 일부러 키우기
        Item item = _items[objectId];
        player.Inventory.TryAdd(item);
        _items.Remove(objectId);
        Console.WriteLine($"[Pick Success?] Room={RoomId}, Player={player.ObjectId}, Item={item.ObjectId}/{item.Name}");

        player.Session.SendPickItem(requestId, ResultCode.Success, item);
    }
}