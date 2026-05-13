using DummyClient.Object;
using Google.Protobuf.Protocol;
using ServerCore;

namespace DummyClient.Session;

public partial class ServerSession : PacketSession
{
    public void HandleSSpawnPlayer(Player player)
    {
        if (IsEnterGame())
            return;

        bool isSuccess = _dummyClient.OnPlayerSpawn(player);
        if (!isSuccess)
        {
            Console.WriteLine("[S_SpawnPlayer] Failed to spawn player");
            return;
        }

        float originX = player.Position.X;
        float originY = player.Position.Y;
        
        _dummyClient.TryMove(originX, originY);
    }

    public void HandleSSpawnItem(Item item)
    {
        if (!IsEnterGame())
            return;

        bool isSuccess = _dummyClient.OnItemSpawned(item);
        if (!isSuccess)
        {
            Console.WriteLine("[S_SpawnItem] Failed to spawn item");
            return;
        }

        _dummyClient.TryPickItem(item.ObjectId);
    }

    public void HandleSPickItem(int requestId, ResultCode resultCode, Item item)
    {
        if (!IsEnterGame())
            return;
        
        _dummyClient.OnPickItem(requestId, resultCode, item);
    }

    private bool IsEnterGame()
    {
        if (_sessionState != SessionState.EnterGame)
        {
            Console.WriteLine("[S_SpawnItem] Invalid State");
            return false;
        }

        return true;
    }
}