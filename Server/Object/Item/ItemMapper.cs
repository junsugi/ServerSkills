using Google.Protobuf.Protocol;

namespace ServerSkills;

public static class ItemMapper
{
    public static ItemInfo ToDto(Item item)
    {
        ItemInfo itemInfo = new ItemInfo()
        {
            ObjectInfo = new ObjectInfo()
            {
                ObjectId = item.ObjectId
            },
            Name = item.Name,
            Count = item.Count
        };

        return itemInfo;
    }
}