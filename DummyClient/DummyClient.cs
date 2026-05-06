using Bogus;
using DummyClient.Session;
using Google.Protobuf.Protocol;
using Player = DummyClient.Object.Player;

namespace DummyClient;

public class DummyClient
{
    public Player MyPlayer {get; private set;}
    
    private ServerSession _serverSession;
    private Faker _faker = new Faker();

    public void SetSession(ServerSession serverSession)
    {
        _serverSession = serverSession;
    }

    public void OnConnected(ResultCode resultCode)
    {
        if (resultCode != ResultCode.Success)
            return;

        C_Login loginPacket = new C_Login();
        loginPacket.Id = _faker.Name.FullName();
        _serverSession.Send(loginPacket);
    }

    public void OnLogin(ResultCode resultCode)
    {
        if (resultCode != ResultCode.Success)
            return;
        
        C_EnterGame enterGamePacket = new C_EnterGame();
        _serverSession.Send(enterGamePacket);
    }

    public void OnEnterGame(ResultCode resultCode, Player? player)
    {
        if (resultCode != ResultCode.Success)
            return;
        
        MyPlayer = player!;
        Console.WriteLine($"{MyPlayer.Id} / {MyPlayer.DisplayName}");
    }
}