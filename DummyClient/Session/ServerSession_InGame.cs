using DummyClient.Object;
using DummyClient.Packet;
using Google.Protobuf.Protocol;
using ServerCore;

namespace DummyClient.Session;

public partial class ServerSession : PacketSession
{
    public void HandleSSpawnPlayer(Player player)
    {
        if (!IsEnterGame())
            return;

        bool isSuccess = _dummyClient.OnPlayerSpawn(player);
        if (!isSuccess)
        {
            Console.WriteLine("[S_SpawnPlayer] Failed to spawn player");
            return;
        }
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

        _dummyClient.TryPickItemDuplicated(item.ObjectId);
    }

    public void HandleSPickItem(int requestId, ResultCode resultCode, Item item)
    {
        if (!IsEnterGame())
            return;
        
        _dummyClient.OnPickItem(requestId, resultCode, item);
    }
    
    public void HandleSMoveHandler(Player player, int requestId, ResultCode resultCode)
    {
        if (!IsEnterGame())
            return;
        
        _dummyClient.OnMove(requestId, resultCode, player);
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