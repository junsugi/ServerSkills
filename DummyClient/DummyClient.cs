using Bogus;
using DummyClient.Packet;
using DummyClient.Session;
using Google.Protobuf.Protocol;
using Player = DummyClient.Object.Player;

namespace DummyClient;

public partial class DummyClient(PendingRequestManager pendingRequestManager)
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
        if (resultCode != ResultCode.Success)
            return;

        C_Login loginPacket = new C_Login();
        loginPacket.Id = _faker.Name.FullName();
        _serverSession.Send(loginPacket);
    }

    public void OnLogin(int requestId, ResultCode resultCode)
    {
        if (resultCode != ResultCode.Success)
            return;

        int enterReqId =
            pendingRequestManager.Register(nameof(C_EnterGame), () => { });
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
        
        // 아이템 스폰 테스트를 위해 더미 패킷 전송
        C_ReadyForTest testPacket = new C_ReadyForTest();
        _serverSession.Send(testPacket);
        
        TryMove();
    }

    private void RequestCompleted(int requestId)
    {
        pendingRequestManager.Completed(requestId);
    }
}