using System.Diagnostics;
using Google.Protobuf.Protocol;
using ServerSkills.Monitoring;

namespace ServerSkills.Processor;

public class DirectEnterGameProcessor(PacketProfiler profiler) : IEnterGameProcessor
{
    public void Process(ClientSession clientSession, int requestId)
    {
        Stopwatch sw = Stopwatch.StartNew();

        try
        {
            if (clientSession.CanEnterGame())
            {
                clientSession.SendEnterGame(requestId, ResultCode.InvalidRequest, null);
                return;
            }

            Player player = PlayerFactory.Create(clientSession.AccountDto!);
            
            Stopwatch addSw = Stopwatch.StartNew();
            ObjectManager.Instance.Add(player);
            addSw.Stop();
            profiler.Record("C_ENTER_GAME.Direct.ObjectManagerAdd", addSw.ElapsedMilliseconds);

            clientSession.SetPlayer(player);
            clientSession.MarkEnterGame();
            clientSession.SendEnterGame(requestId, ResultCode.Success, player);
        }
        finally
        {
            sw.Stop();
            profiler.Record("C_ENTER_GAME.Direct.Total", sw.ElapsedMilliseconds);
        }
    }
}