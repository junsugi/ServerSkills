using System.Diagnostics;
using Google.Protobuf.Protocol;
using ServerSkills.Login;
using ServerSkills.Monitoring;

namespace ServerSkills;

public partial class ClientSession(IAccountService accountService, PacketProfiler profiler) : PacketSession
{
    public Player MyPlayer;

    private SessionState _sessionState = SessionState.None;
    private AccountDto? _accountDto;
    
    private object _lock = new object();
    
    public void HandleCConnected()
    {
        _sessionState = SessionState.Connected;
        
        S_Connected connectedPacket = new S_Connected();
        connectedPacket.Result = ResultCode.Success;
        Send(connectedPacket);
    }

    public void HandleCLogin(int requestId, string id)
    {
        S_Login loginPacket = new S_Login();
        loginPacket.RequestId = requestId;
        
        if (_sessionState != SessionState.Connected)
        {
            loginPacket.Result = ResultCode.InvalidRequest;
            loginPacket.RequestId = requestId;
            Send(loginPacket);
            return;
        }
            
        LoginResult loginResult = accountService.Login(id, out AccountDto? accountDto);
        
        if (loginResult != LoginResult.Success)
        {
            loginPacket.Result = ResultCode.NotFound;
            Send(loginPacket);
            return;
        }
        
        _sessionState = SessionState.Authenticated;
        _accountDto = accountDto;
        loginPacket.Result = ResultCode.Success;
        Send(loginPacket);
    }

    public void HandleCEnterGame(int requestId)
    {
        Stopwatch sw = Stopwatch.StartNew();

        try
        {
            S_EnterGame enterGamePacket = new S_EnterGame();
            enterGamePacket.RequestId = requestId;

            if (_sessionState != SessionState.Authenticated)
            {
                enterGamePacket.Result = ResultCode.InvalidRequest;
                enterGamePacket.RequestId = requestId;
                Send(enterGamePacket);
                return;
            }

            MyPlayer = PlayerFactory.Create(_accountDto!);
            ObjectManager.Instance.Add(MyPlayer);

            enterGamePacket.Player = PlayerMapper.ToDto(MyPlayer);
            enterGamePacket.Result = ResultCode.Success;
            Send(enterGamePacket);
        }
        finally
        {
            sw.Stop();
            profiler.Record("S_ENTER_GAME.Direct", sw.ElapsedMilliseconds);
        }
    }
}