using DummyClient.Session;
using Google.Protobuf.Protocol;

namespace DummyClient;

public class DummyClient
{
    private ServerSession _serverSession;
    
    public void SetSession(ServerSession serverSession)
    {
        _serverSession = serverSession;
    }

    public void OnConnected(ResultCode resultCode)
    {
        if (resultCode == ResultCode.Success)
        {
            Console.WriteLine($"{resultCode}");
        }
    }
}