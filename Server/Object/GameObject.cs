using ServerSkills.Game.Room;

namespace ServerSkills;

public class GameObject
{
    public int ObjectId { get; set; }
    public GameObjectType ObjectType { get; set; }
    public GameRoom GameRoom { get; set; }
    public ClientSession Session { get; set; }
}