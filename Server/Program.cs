using System.Net;
using Google.Protobuf.Protocol;
using ServerCore;
using ServerSkills.Login;
using ServerSkills.Monitoring;
using ServerSkills.Processor;

namespace ServerSkills;

class Program
{
    public static Listener listener;
    public static PacketProfiler _profiler = new PacketProfiler();
    static void Main(string[] args)
    {
        
        
        IAccountRepository accountRepository = new FakeAccountRepository();
        IAccountService accountService = new AccountService(accountRepository);
        // 테스트용 갈아끼우기
        IEnterGameProcessor enterGameProcessor = new DirectEnterGameProcessor(_profiler);
        
        IPEndPoint endPoint = new IPEndPoint(IPAddress.Loopback, 5555);
        
        listener = new Listener();
        listener.Init(endPoint, () => new ClientSession(accountService, enterGameProcessor));

        Console.WriteLine($"Listening on {endPoint.Address}:{endPoint.Port}");

        // 모니터링 시작
        Task.Run(MonitoringLoop);
        
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