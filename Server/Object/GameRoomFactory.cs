using ServerSkills.Game.Room;
using ServerSkills.Monitoring;

namespace ServerSkills;

public static class GameRoomFactory
{
    public static GameRoom Create(int roomId)
    {
        PickItemMetrics metrics = new PickItemMetrics();
        // 여기서 전략 수정
        return new GameRoom(roomId, new ClaimPickItemStrategy(metrics), metrics);
    }
}