using ServerSkills.Game.Room;

namespace ServerSkills;

public interface IPickItemStrategy
{
    void Pick(GameRoom gameRoom, Player player, int requestId, int objectId);
}