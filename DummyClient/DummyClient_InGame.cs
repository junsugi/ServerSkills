using DummyClient.Object;
using Google.Protobuf.Protocol;

namespace DummyClient;

public partial class DummyClient
{
    private Dictionary<int, Item> _items = new Dictionary<int, Item>();
    
    public bool OnItemSpawned(Item item)
    {
        return _items.TryAdd(item.ObjectId, item);
    }
    
    public void TryPickItem(int itemObjectId)
    {
        if (!IsItemExist(itemObjectId))
        {
            Console.WriteLine($"Item {itemObjectId} doesn't exist");
            return;
        }
        
        C_PickItem pickItemPacket = new C_PickItem()
        {
            RequestId = pendingRequestManager.Register(nameof(C_PickItem), () => { }),
            ObjectId = itemObjectId,
        };
        
        _serverSession.Send(pickItemPacket);
    }

    public void OnPickItem(int requestId, ResultCode resultCode, Item? item)
    {
        pendingRequestManager.Completed(requestId);

        if (resultCode != ResultCode.Success || item == null)
        {
            Console.WriteLine($"Item {requestId} failed");
            return;
        }

        Console.WriteLine($"Player: [{MyPlayer.ObjectId}],  Item {item.ObjectId} picked");
        _items.Remove(item.ObjectId);
    }

    private bool IsItemExist(int itemObjectId)
    {
        return _items.ContainsKey(itemObjectId);
    }
}