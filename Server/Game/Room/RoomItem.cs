namespace ServerSkills;

public class RoomItem(Item item)
{
    public int ObjectId { get; } = item.ObjectId;
    public Item Item { get; } = item;

    private int _claimedByPlayerId;
    public bool TryClaim(int playerId)
    {
        return Interlocked.CompareExchange(ref _claimedByPlayerId, playerId, 0) == 0;
    }

    public bool RollbackClaim(int playerId)
    {
        return Interlocked.CompareExchange(ref _claimedByPlayerId, 0,playerId) == playerId;
    }

    public int GetClaimedByPlayerId() => _claimedByPlayerId;
}