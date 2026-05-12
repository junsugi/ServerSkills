namespace ServerSkills;

public class Inventory
{
    private Dictionary<int, Item> _slots = new();

    public bool TryAdd(Item item)
    {
        // 테스트 편의를 위해 1종류의 아이템들만 가질 수 있다고 가정.
        return _slots.TryAdd(item.ObjectId, item);
    }

    public bool IsFull()
    {
        return _slots.Count == 10;
    }
}