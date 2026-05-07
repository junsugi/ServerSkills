using Bogus;
using DummyClient.Packet;
using DummyClient.Session;
using Google.Protobuf.Protocol;
using ServerSkills.Monitoring;
using Player = DummyClient.Object.Player;

namespace DummyClient;

public class DummyClient(PendingRequestManager pendingRequestManager)
{
    public Player MyPlayer { get; private set; }

    private ServerSession _serverSession;
    private Faker _faker = new Faker();

    public void SetSession(ServerSession serverSession)
    {
        _serverSession = serverSession;
    }

    public void OnConnected(int requestId, ResultCode resultCode)
    {
        RequestCompleted(requestId);

        if (resultCode != ResultCode.Success)
            return;

        int loginReqId = pendingRequestManager.Register("C_LOGIN", () => { });
        C_Login loginPacket = new C_Login();
        loginPacket.Id = _faker.Name.FullName();
        loginPacket.RequestId = loginReqId;
        _serverSession.Send(loginPacket);
    }

    public void OnLogin(int requestId, ResultCode resultCode)
    {
        RequestCompleted(requestId);

        if (resultCode != ResultCode.Success)
            return;

        int enterReqId =
            pendingRequestManager.Register("C_ENTER_GAME", () => { });
        C_EnterGame enterGamePacket = new C_EnterGame();
        enterGamePacket.RequestId = enterReqId;
        _serverSession.Send(enterGamePacket);
    }

    public void OnEnterGame(int requestId, ResultCode resultCode, Player? player)
    {
        RequestCompleted(requestId);

        if (resultCode != ResultCode.Success)
            return;

        MyPlayer = player!;
        // Console.WriteLine($"{MyPlayer.ObjectType} / {MyPlayer.NickName}");
    }

    private void RequestCompleted(int requestId)
    {
        pendingRequestManager.Completed(requestId);
    }
}