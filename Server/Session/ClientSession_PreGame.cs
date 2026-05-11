using System.Diagnostics;
using Google.Protobuf.Protocol;
using ServerCore;
using ServerSkills.Game.Room;
using ServerSkills.Login;
using ServerSkills.Monitoring;

namespace ServerSkills;

public enum EnterGameMode
{
    DirectLock,
    ObjectManagerJobQueue,
    GameRoomJobQueue
}

public partial class ClientSession(
    EnterGameMode enterGameMode,
    IAccountService accountService, 
    PacketProfiler profiler) : PacketSession
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
    
    public void HandleCReadyForTest()
    {
        TestManager testManager = TestManager.Instance;
        testManager.IncreasePlayer();
        
        if (!testManager.IsDoneTargetPlayer())
            return;
        
        Console.WriteLine("[C_READY_FOR_TEST] Done!");
        GameRoomManager.Instance.SpawnItemAll();
    }

    #region EnterGame
    
    public void HandleCEnterGame(int requestId)
    {
        switch (enterGameMode)
        {
            case EnterGameMode.DirectLock:
                HandleCEnterGameDirectLock(requestId);
                break;
            case EnterGameMode.ObjectManagerJobQueue:
                HandleCEnterGameObjectManagerJobQueue(requestId);
                break;
            case EnterGameMode.GameRoomJobQueue:
                HandleCEnterGameRoomJobQueue(requestId);
                break;
            default:
                SendEnterGame(requestId, ResultCode.InvalidRequest, null);
                break;
        }
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
    
    private void HandleCEnterGameDirectLock(int requestId)
    {
        long startAt = Stopwatch.GetTimestamp();

        if (!TryPrepareEnterGame(requestId, out Player? player))
            return;

        long addStartAt = Stopwatch.GetTimestamp();
        ObjectManager.Instance.Add(player!); // lock 있는 Add
        long addEndAt = Stopwatch.GetTimestamp();

        SetPlayer(player!);
        MarkEnterGame();
        SendEnterGame(requestId, ResultCode.Success, player);

        long endAt = Stopwatch.GetTimestamp();

        profiler.Record("C_ENTER_GAME.Direct.ObjectManagerAdd", ToMs(addEndAt - addStartAt));
        profiler.Record("C_ENTER_GAME.Direct.Total", ToMs(endAt - startAt));
    }
    
    private void HandleCEnterGameObjectManagerJobQueue(int requestId)
    {
        long handleStartAt = Stopwatch.GetTimestamp();

        if (!TryPrepareEnterGame(requestId, out Player? player))
            return;

        long beforeEnqueue = Stopwatch.GetTimestamp();

        ObjectManager.Instance.AddQueued(player!, (addedObject, metrics) =>
        {
            Player addedPlayer = (Player)addedObject;

            SetPlayer(addedPlayer);
            MarkEnterGame();
            SendEnterGame(requestId, ResultCode.Success, addedPlayer);

            long responseSentAt = Stopwatch.GetTimestamp();

            profiler.Record("C_ENTER_GAME.ObjectManagerJob.QueueWait", ToMs(metrics.StartAt - metrics.EnqueueAt));
            profiler.Record("C_ENTER_GAME.ObjectManagerJob.Execute", ToMs(metrics.EndAt - metrics.StartAt));
            profiler.Record("C_ENTER_GAME.ObjectManagerJob.Total", ToMs(metrics.EndAt - metrics.EnqueueAt));
            profiler.Record("C_ENTER_GAME.ObjectManagerJob.ResponseTotal", ToMs(responseSentAt - handleStartAt));
        });

        long afterEnqueue = Stopwatch.GetTimestamp();
        profiler.Record("C_ENTER_GAME.ObjectManagerJob.Enqueue", ToMs(afterEnqueue - beforeEnqueue));
    }
    
    private void HandleCEnterGameRoomJobQueue(int requestId)
    {
        long handleStartAt = Stopwatch.GetTimestamp();
        
        if (!TryPrepareEnterGame(requestId, out Player? player))
            return;
        
        // 게임룸 찾기
        GameRoom gameRoom = GameRoomManager.Instance.PickRoom(player!);
        
        long beforeEnqueue = Stopwatch.GetTimestamp();
        
        gameRoom.EnterGame(requestId, player, (roomId, metrics) =>
        {
            long responseSentAt = Stopwatch.GetTimestamp();

            profiler.Record($"C_ENTER_GAME.RoomJob.Room{roomId}.QueueWait", ToMs(metrics.StartAt - metrics.EnqueueAt));
            profiler.Record($"C_ENTER_GAME.RoomJob.Room{roomId}.Execute", ToMs(metrics.EndAt - metrics.StartAt));
            profiler.Record($"C_ENTER_GAME.RoomJob.Room{roomId}.Total", ToMs(metrics.EndAt - metrics.EnqueueAt));

            profiler.Record("C_ENTER_GAME.RoomJob.QueueWait", ToMs(metrics.StartAt - metrics.EnqueueAt));
            profiler.Record("C_ENTER_GAME.RoomJob.Execute", ToMs(metrics.EndAt - metrics.StartAt));
            profiler.Record("C_ENTER_GAME.RoomJob.Total", ToMs(metrics.EndAt - metrics.EnqueueAt));
            profiler.Record("C_ENTER_GAME.RoomJob.ResponseTotal", ToMs(responseSentAt - handleStartAt));
        });
        
        long afterEnqueue = Stopwatch.GetTimestamp();
        profiler.Record("C_ENTER_GAME.RoomJob.Enqueue", ToMs(afterEnqueue - beforeEnqueue));
    }
    
    private bool TryPrepareEnterGame(int requestId, out Player? player)
    {
        player = null;

        if (!CanEnterGame())
        {
            SendEnterGame(requestId, ResultCode.InvalidRequest, null);
            return false;
        }

        player = PlayerFactory.Create(AccountDto!);
        player.Session = this;
        return true;
    }

    private bool CanEnterGame()
    {
        return _sessionState == SessionState.Authenticated;
    }
    
    
    private static long ToMs(long tick)
    {
        return tick * 1000 / Stopwatch.Frequency;
    }
    
    #endregion
}