namespace ServerSkills;

public class ObjectManager
{
    public static readonly ObjectManager Instance = new();

    private Dictionary<int, Player> _players = new();
    private object _lock = new();

    private ObjectManager()
    {
    }

    public GameObject Add(GameObject gameObject)
    {
        lock (_lock)
        {
            GameObjectType type = GetObjectTypeById(gameObject.ObjectId);

            if (type == GameObjectType.PLAYER)
            {
                Player player = gameObject as Player;
                gameObject.ObjectId = GenerateId(gameObject.ObjectType, player.ObjectId);
                _players.Add(gameObject.ObjectId, gameObject as Player);
            }

            return gameObject;
        }
    }

    public int GenerateId(GameObjectType type, int id)
    {
        return ((int)type << 24) | id;
    }

    public static GameObjectType GetObjectTypeById(int id)
    {
        int type = (id >> 24) & 0x7F;
        return (GameObjectType)type;
    }

    public Player Find(int objectId)
    {
        GameObjectType objectType = GetObjectTypeById(objectId);

        lock (_lock)
        {
            if (objectType == GameObjectType.PLAYER)
            {
                if (_players.TryGetValue(objectId, out var player))
                    return player;
            }
        }

        return null;
    }
}