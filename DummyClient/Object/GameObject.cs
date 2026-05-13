using DummyClient.Object;

namespace DummyClient;

public class GameObject
{
    public int ObjectId { get; set; }
    public GameObjectType ObjectType { get; set; }
    public string NickName { get; set; }
    public Position Position { get; set; }
}