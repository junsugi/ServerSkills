namespace ServerSkills;

public static class ItemFactory
{
    public static Item Create(CreateItemRequest request)
    {
        return new Item()
        {
            ObjectId = request.ObjectId,
            ObjectType = GameObjectType.ITEM,
            Name = request.Name,
            Count = request.Count,
        };
    }
}