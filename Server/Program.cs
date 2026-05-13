using System.Net;
using ServerCore;
using ServerSkills.Game.Room;
using ServerSkills.Login;
using ServerSkills.Monitoring;

namespace ServerSkills;

class Program
{
    public static Listener listener;
    public static PacketProfiler _profiler = new PacketProfiler();

    static void Main(string[] args)
    {
        Dictionary<int, GameRoom> gameRooms = GameRoomManager.Instance.Creates(4);

        IAccountRepository accountRepository = new FakeAccountRepository();
        IAccountService accountService = new AccountService(accountRepository);

        IPEndPoint endPoint = new IPEndPoint(IPAddress.Loopback, 5555);

        listener = new Listener();
        listener.Init(endPoint, () => new ClientSession(EnterGameMode.GameRoomJobQueue, accountService, _profiler));

        Console.WriteLine($"Listening on {endPoint.Address}:{endPoint.Port}");

        // 모니터링 시작
        Task.Run(() => MonitoringLoop(gameRooms));

        // Queue Flush
        Task.Run(async () =>
        {
            while (true)
            {
                ObjectManager.Instance.Flush();
                await Task.Delay(1);
            }
        });

        foreach (GameRoom gameRoom in gameRooms.Values)
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        gameRoom.Flush();
                        await Task.Delay(1);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }
            });
        }

        Thread.Sleep(Timeout.Infinite);
    }

    private static async Task MonitoringLoop(Dictionary<int, GameRoom> gameRooms)
    {
        while (true)
        {
            await Task.Delay(5000);

            // foreach (PacketProfileSnapshot snapshot in _profiler.SnapshotAndClearAll())
            // {
            //     Console.WriteLine(
            //         $"[{snapshot.Name}] " +
            //         $"Count={snapshot.Count}, " +
            //         $"Avg={snapshot.Avg:F2}ms, " +
            //         $"P95={snapshot.P95}ms, " +
            //         $"P99={snapshot.P99}ms, " +
            //         $"Max={snapshot.Max}ms"
            //     );
            // }
            
            foreach (GameRoom room in gameRooms.Values.OrderBy(r => r.RoomId))
            {
                // room.PickMetrics.Print(room.RoomId);
                
                MoveMetricSnapshot snapshot = room.MoveMetrics.SnapshotAndClear();

                double requestsPerSec = snapshot.MoveRequests / 5.0;
                double sendsPerSec = snapshot.MoveSends / 5.0;
                
                Console.WriteLine(
                    $"[Room {room.RoomId} Move] " +
                    $"Requests={requestsPerSec:F1}, " +
                    $"Sends={sendsPerSec:F1}, " +
                    $"AvgRecipients={snapshot.AvgRecipients:F2}"
                );
            }

            GameRoomManager.Instance.PrintRoomStatus();
        }
    }
}