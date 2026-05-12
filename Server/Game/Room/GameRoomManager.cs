using Google.Protobuf.Protocol;

namespace ServerSkills.Game.Room;

public class GameRoomManager
{
    public static readonly GameRoomManager Instance = new();

    private Dictionary<int, GameRoom> _rooms = new();
    private int _roomId = 1;

    private GameRoomManager()
    {
    }

    // 이상해? CreateItemRequest가 매니저 안에 있는게?
    public void SpawnItemAll()
    {
        foreach (GameRoom room in _rooms.Values)
        {
            CreateItemRequest request = new CreateItemRequest();
            request.ObjectId = ObjectManager.Instance.GenerateId(GameObjectType.ITEM, 101);
            request.Name = "집행검";
            request.Count = 1;
            
            Item item = ItemFactory.Create(request);
            room.SpawnItem(item);
        }
    }
    
    public Dictionary<int, GameRoom> Creates(int roomCount = 1)
    {
        for (int i = 0; i < roomCount; i++)
            _rooms.Add(i, GameRoomFactory.Create(i));

        return _rooms;
    }

    public bool Remove(int roomId)
    {
        return _rooms.Remove(roomId);
    }

    public GameRoom PickRoom(Player player)
    {
        int index = Math.Abs(player.ObjectId) % _rooms.Count;
        return _rooms[index];
    }

    public void PrintRoomStatus()
    {
        foreach (GameRoom room in _rooms.Values.OrderBy(r => r.RoomId))
        {
            Console.Write($"[Room {room.RoomId}: Players={room.PlayerCount}] |");
        }

        Console.WriteLine($"Total Rooms={_rooms.Count}, Total Players={_rooms.Values.Sum(r => r.PlayerCount)}");
    }
}

