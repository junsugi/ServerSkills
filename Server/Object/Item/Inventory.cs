namespace ServerSkills;

public class Inventory
{
    private Dictionary<int, Item> _slots = new();

    public bool TryAdd(Item item)
    {
        if (IsFull())
            return false;

        if (HasItem(item))
            return false;
        
        // 테스트 편의를 위해 1종류의 아이템들만 가질 수 있다고 가정.
        return _slots.TryAdd(item.ObjectId, item);
    }

    private bool IsFull() => _slots.Count == 10;
    private bool HasItem(Item item) => _slots.ContainsKey(item.ObjectId);
}