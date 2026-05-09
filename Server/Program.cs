using System.Net;
using ServerCore;
using ServerSkills.Login;
using ServerSkills.Monitoring;

namespace ServerSkills;

class Program
{
    public static Listener listener;
    public static PacketProfiler _profiler = new PacketProfiler();
    static void Main(string[] args)
    {
        IAccountRepository accountRepository = new FakeAccountRepository();
        IAccountService accountService = new AccountService(accountRepository);
        
        IPEndPoint endPoint = new IPEndPoint(IPAddress.Loopback, 5555);
        
        listener = new Listener();
        listener.Init(endPoint, () => new ClientSession(EnterGameMode.ObjectManagerJobQueue, accountService, _profiler));

        Console.WriteLine($"Listening on {endPoint.Address}:{endPoint.Port}");

        // 모니터링 시작
        Task.Run(MonitoringLoop);

        // Queue Flush
        Task.Run(async () =>
        {
            while (true)
            {
                ObjectManager.Instance.Flush();
                await Task.Delay(1);
            }
        });
        
        while (true)
        {
        }
    }

    private static async Task MonitoringLoop()
    {
        while (true)
        {
            await Task.Delay(5000);
            
            foreach (PacketProfileSnapshot snapshot in _profiler.SnapshotAndClearAll())
            {
                Console.WriteLine(
                    $"[{snapshot.Name}] " +
                    $"Count={snapshot.Count}, " +
                    $"Avg={snapshot.Avg:F2}ms, " +
                    $"P95={snapshot.P95}ms, " +
                    $"P99={snapshot.P99}ms, " +
                    $"Max={snapshot.Max}ms"
                );
            }
        }
    }
}