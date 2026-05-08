using System.Diagnostics;
using Google.Protobuf.Protocol;
using ServerSkills.Login;
using ServerSkills.Monitoring;
using ServerSkills.Processor;

namespace ServerSkills;

public partial class ClientSession(
    IAccountService accountService, 
    IEnterGameProcessor enterGameProcessor) : PacketSession
{
    public Player MyPlayer;
    public AccountDto? AccountDto;

    private SessionState _sessionState = SessionState.None;
    private object _lock = new object();
    
    public void SetPlayer(Player player)
    {
        MyPlayer = player;
    }
    
    public void MarkEnterGame()
    {
        _sessionState = SessionState.EnterGame;
    }
    
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
        AccountDto = accountDto;
        loginPacket.Result = ResultCode.Success;
        Send(loginPacket);
    }

    public void HandleCEnterGame(int requestId)
    {
        enterGameProcessor.Process(this, requestId);
    }

    public bool CanEnterGame()
    {
        return _sessionState != SessionState.Authenticated;
    }

    public void SendEnterGame(int requestId, ResultCode resultCode, Player? player)
    {
        S_EnterGame enterGamePacket = new S_EnterGame()
        {
            Result = resultCode,
            RequestId = requestId,
        };
        
        if (player != null)
            enterGamePacket.Player = PlayerMapper.ToDto(player);
        
        Send(enterGamePacket);
    }
}