using Google.Protobuf.Protocol;
using ServerSkills.Login;

namespace ServerSkills;

public partial class ClientSession(IAccountService accountService) : PacketSession
{
    private SessionState _sessionState = SessionState.None;
    private AccountDto? _accountDto;
    private Dictionary<string, Player> _players = new Dictionary<string, Player>();
    
    private object _lock = new object();
    
    public void HandleCConnected()
    {
        _sessionState = SessionState.Connected;
        
        S_Connected connectedPacket = new S_Connected();
        connectedPacket.Result = ResultCode.Success;
        Send(connectedPacket);
    }

    public void HandleCLogin(string id)
    {
        S_Login loginPacket = new S_Login();
        
        if (_sessionState != SessionState.Connected)
        {
            loginPacket.Result = ResultCode.InvalidRequest;
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

    public void HandleCEnterGame()
    {
        S_EnterGame enterGamePacket = new S_EnterGame();
        
        if (_sessionState != SessionState.Authenticated)
        {
            enterGamePacket.Result = ResultCode.InvalidRequest;
            Send(enterGamePacket);
            return;
        }

        Player player = PlayerFactory.Create(_accountDto!);
        lock (_lock)
        {
            _players.Add(player.Id, player);
            Console.WriteLine($"{player.Id}: 추가! / {_players.Count}");
        }
        enterGamePacket.Player = PlayerMapper.ToDto(player);
        enterGamePacket.Result = ResultCode.Success;
        Send(enterGamePacket);
    }
}