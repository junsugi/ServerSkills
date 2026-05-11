using Google.Protobuf.Protocol;

namespace DummyClient.Object;

public static class ItemMapper
{
    public static Item ToDomain(ItemInfo itemInfo)
    {
        Item item = new Item()
        {
            ObjectId = itemInfo.ObjectInfo.ObjectId,
            Count = itemInfo.Count
        };

        return item;
    }
}