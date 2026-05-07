using Google.Protobuf.Protocol;
using ServerSkills;
using Player = DummyClient.Object.Player;
using PlayerMapper = DummyClient.Object.PlayerMapper;

namespace DummyClient.Session;

public partial class ServerSession : PacketSession
{
    private DummyClient _dummyClient;
    private SessionState _sessionState = SessionState.None;
   
    public void SetClient(DummyClient dummyClient)
    {
        _dummyClient = dummyClient;
    }

    public void HandleSConnected(int requestId, ResultCode resultCode)
    {
        _dummyClient.OnConnected(requestId, resultCode);

        bool isSuccess = resultCode == ResultCode.Success;
        _sessionState = isSuccess
            ? SessionState.Connected
            : SessionState.None;
    }

    public void HandleSLogin(int requestId, ResultCode resultCode)
    {
        _dummyClient.OnLogin(requestId, resultCode);

        bool isSuccess = resultCode == ResultCode.Success;
        _sessionState = isSuccess
            ? SessionState.Authenticated
            : SessionState.Connected;
    }

    public void HandleSEnterGame(int requestId, ResultCode resultCode, PlayerInfo playerInfo)
    {
        Player? player = resultCode == ResultCode.Success
            ? PlayerMapper.ToDomain(playerInfo)
            : null;

        _sessionState = resultCode == ResultCode.Success
            ? SessionState.EnterGame
            : SessionState.Authenticated;

        _dummyClient.OnEnterGame(requestId, resultCode, player);
    }
}