using System.Collections.Concurrent;

namespace ServerSkills.Monitoring;

public class PickItemMetrics
{
    public int TotalRequest;
    public int Success;
    public int ClaimFail;
    public int CommitFail;
    public int RollbackCount;
    public int DuplicateRequest;
    public int DoubleGrant;

    private readonly ConcurrentDictionary<(int playerId, int itemId), byte> _requests = new();
    private readonly ConcurrentDictionary<int, ConcurrentDictionary<int, byte>> _grants = new();

    public void OnRequest(int playerId, int itemId)
    {
        Interlocked.Increment(ref TotalRequest);

        if (!_requests.TryAdd((playerId, itemId), 0))
            Interlocked.Increment(ref DuplicateRequest);
    }

    public void OnSuccess(int playerId, int itemId)
    {
        Interlocked.Increment(ref Success);

        var players = _grants.GetOrAdd(itemId, _ => new ConcurrentDictionary<int, byte>());
        players.TryAdd(playerId, 0);

        if (players.Count > 1)
            Interlocked.Increment(ref DoubleGrant);
    }

    public int GetGrantedPlayerCount(int itemId)
    {
        return _grants.TryGetValue(itemId, out var players) ? players.Count : 0;
    }

    public void Print(int roomId)
    {
        Console.WriteLine(
            $"[PICK_ITEM_METRICS] roomId={roomId}, " +
            $"TotalRequest={Volatile.Read(ref TotalRequest)}, " +
            $"Success={Volatile.Read(ref Success)}, " +
            $"ClaimFail={Volatile.Read(ref ClaimFail)}, " +
            $"CommitFail={Volatile.Read(ref CommitFail)}, " +
            $"RollbackCount={Volatile.Read(ref RollbackCount)}, " +
            $"DuplicateRequest={Volatile.Read(ref DuplicateRequest)}, " +
            $"DoubleGrant={Volatile.Read(ref DoubleGrant)}"
        );
    }
}