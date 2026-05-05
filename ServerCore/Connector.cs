using System.Net;
using System.Net.Sockets;
using ServerCore;

namespace DummyClient;

public class Connector
{
    private Func<Session> _sessionFactory;

    public void Connect(EndPoint endPoint, Func<Session> sessionFactory, int count = 1)
    {
        for (int i = 0; i < count; i++)
        {
            Socket socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            _sessionFactory += sessionFactory;
        
            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
            args.Completed += OnConnectCompleted;
            args.RemoteEndPoint = endPoint;
            args.UserToken = socket;

            RegisterConnect(args);
            
            Thread.Sleep(100);
        }
    }

    private void RegisterConnect(SocketAsyncEventArgs args)
    {
        Socket socket = args.UserToken as Socket;
        if (socket == null)
            return;

        try
        {
            bool pending = socket.ConnectAsync(args);
            if (!pending)
                OnConnectCompleted(null, args);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    private void OnConnectCompleted(object sender, SocketAsyncEventArgs args)
    {
        try
        {
            if (args.SocketError == SocketError.Success)
            {
                Session session = _sessionFactory.Invoke();
                session.Start(args.ConnectSocket);
                session.OnConnected();
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
}