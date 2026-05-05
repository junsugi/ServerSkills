using System.Net;
using DummyClient.Session;

namespace DummyClient;

class Program
{
    static void Main(string[] args)
    {
        IPEndPoint endPoint = new IPEndPoint(IPAddress.Loopback, 5555);
        
        Connector connector = new Connector();
        connector.Connect(endPoint, () =>
        {
            DummyClient client = new DummyClient();
            ServerSession serverSession = new ServerSession();

            client.SetSession(serverSession);
            serverSession.SetClient(client);
            
            return serverSession;
        }, 10);
    }
}