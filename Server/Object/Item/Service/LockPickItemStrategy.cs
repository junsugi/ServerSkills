using Google.Protobuf.Protocol;
using ServerSkills.Game.Room;

namespace ServerSkills;

public class LockPickItemStrategy : IPickItemStrategy
{
    private static object _lock = new();
    
    public void Pick(GameRoom gameRoom, Player player, int requestId, int objectId)
    {
        Item item = null;
        
        lock (_lock)
        {
            if (!gameRoom.HasRoomItem(objectId))
            {
                Console.WriteLine($"[Pick Failed] Room={gameRoom.RoomId}, Player={player.ObjectId}, Item={objectId}");
                gameRoom.CompletePickItemRequest(player, requestId, objectId, ResultCode.InvalidRequest);
                return;
            }

            RoomItem roomItem = gameRoom.GetRoomItem(objectId)!;
            item = roomItem.Item;
        
            Thread.Sleep(100); // 경합 상황 일부러 키우기
            player.Inventory.TryAdd(item);
            gameRoom.RemoveRoomItem(objectId);
            Console.WriteLine($"[Pick Success?] Room={gameRoom.RoomId}, Player={player.ObjectId}, Item={item.ObjectId}/{item.Name}");

        }
        
        gameRoom.CompletePickItemRequest(player, requestId, objectId, ResultCode.Success, item);
    }
}