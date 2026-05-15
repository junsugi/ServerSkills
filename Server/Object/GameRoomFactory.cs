using ServerSkills.Game.Room;
using ServerSkills.Monitoring;

namespace ServerSkills;

public static class GameRoomFactory
{
    public static GameRoom Create(int roomId, PickItemMode mode)
    {
        PickItemMetrics pickMetrics = new PickItemMetrics();
        MoveMetrics moveMetrics = new MoveMetrics();
        
        // 여기서 전략 수정
        switch (mode)
        {
            case PickItemMode.Unsafe:
                return new GameRoom(roomId, mode, new UnsafePickItemStrategy(), pickMetrics, moveMetrics);
            case PickItemMode.Lock:
                return new GameRoom(roomId, mode, new LockPickItemStrategy(), pickMetrics, moveMetrics);
            case PickItemMode.Claim:
                return new GameRoom(roomId, mode, new ClaimPickItemStrategy(pickMetrics), pickMetrics, moveMetrics);
            default:
                return new GameRoom(roomId, mode, new UnsafePickItemStrategy(), pickMetrics, moveMetrics);
        }
    }
}