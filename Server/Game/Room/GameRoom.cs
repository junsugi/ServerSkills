using System.Diagnostics;
using Google.Protobuf.Protocol;
using ServerSkills.Job;

namespace ServerSkills.Game.Room;

public class GameRoom(int roomId) : JobSerializer
{
    public int RoomId {get; set;} = roomId;
    public int PlayerCount => _players.Count;

    private Dictionary<int, Player> _players = new Dictionary<int, Player>();

    public void EnterGame(int requestId, GameObject? gameObject, Action<int, JobMetrics> onCompleted)
    {
        long enqueueAt = Stopwatch.GetTimestamp();
        
        Push(() =>
        {
            Thread.Sleep(100);
            long startAt = Stopwatch.GetTimestamp();
            
            if (gameObject == null)
                Console.WriteLine($"GameObject is Null..");
        
            GameObjectType type = ObjectManager.GetObjectTypeById(gameObject!.ObjectId);
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
}