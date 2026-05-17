using System.Net;
using DummyClient.Packet;
using DummyClient.Session;
using ServerCore;
using ServerSkills.Monitoring;

namespace DummyClient;

class Program
{
    static void Main(string[] args)
    {
        IPEndPoint endPoint = new IPEndPoint(IPAddress.Loopback, 5555);

        LatencyTracker latencyTracker = new LatencyTracker();
        PendingRequestManager pendingRequestManager = new(latencyTracker);

        Task.Run(async () =>
        {
            while (true)
            {
                await Task.Delay(5000);
                foreach (LatencySnapshot snapshot in latencyTracker.SnapshotAndClearAll())
                {
                    Console.WriteLine(
                        $"[{snapshot.Name} RTT] " +
                        $"Count={snapshot.Count}, " +
                        $"Avg={snapshot.Avg:F2}ms, " +
                        $"P95={snapshot.P95}ms, " +
                        $"P99={snapshot.P99}ms, " +
                        $"Max={snapshot.Max}ms"
                    );
                }
            }
        });

        Func<ServerCore.Session> createSession = () =>
        {
            DummyClient client = new DummyClient(pendingRequestManager);
            ServerSession serverSession = new ServerSession();

            client.SetSession(serverSession);
            serverSession.SetClient(client);

            return serverSession;
        };

        int total = 100;
        int workerCount = 5;
        int perWorker = total / workerCount;
        int delayMs = 50;

        List<Task> tasks = new();

        for (int i = 0; i < workerCount; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                try
                {
                    Connector connector = new Connector();
                    connector.Connect(endPoint, createSession, perWorker, delayMs);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }));
        }

        Task.WaitAll(tasks.ToArray());
        Thread.Sleep(Timeout.Infinite);
    }
}