using DummyClient.Object;
using Google.Protobuf.Protocol;
using ServerCore;

namespace DummyClient.Session;

public partial class ServerSession : PacketSession
{
    public void HandleSSpawnPlayer(Player player)
    {
        // TODO
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