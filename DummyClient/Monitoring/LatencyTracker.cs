using System.Collections.Concurrent;
using System.Diagnostics;

namespace ServerSkills.Monitoring;

public sealed class LatencyTracker
{
    private readonly ConcurrentDictionary<int, PendingRequest> _pendingRequests = new();
    private readonly ConcurrentDictionary<string, ConcurrentQueue<long>> _samples = new();

    public void Start(int requestId, string name)
    {
        _pendingRequests[requestId] = new PendingRequest(name, Stopwatch.GetTimestamp());
    }

    public bool Complete(int requestId)
    {
        if (!_pendingRequests.TryRemove(requestId, out PendingRequest pending))
            return false;

        long end = Stopwatch.GetTimestamp();
        long elapsedMs = (end - pending.StartTimestamp) * 1000 / Stopwatch.Frequency;

        ConcurrentQueue<long> queue =
            _samples.GetOrAdd(pending.Name, _ => new ConcurrentQueue<long>());
        
        queue.Enqueue(elapsedMs);
        return true;
    }
    
    public IEnumerable<LatencySnapshot> SnapshotAndClearAll()
    {
        foreach (var pair in _samples)
        {
            List<long> values = new();

            while (pair.Value.TryDequeue(out long elapsedMs))
                values.Add(elapsedMs);

            if (values.Count == 0)
                continue;

            values.Sort();

            yield return new LatencySnapshot
            {
                Name = pair.Key,
                Count = values.Count,
                Avg = values.Average(),
                Min = values[0],
                Max = values[^1],
                P95 = Percentile(values, 0.95),
                P99 = Percentile(values, 0.99)
            };
        }
    }
    
    private static long Percentile(List<long> sorted, double percentile)
    {
        int index = (int)Math.Ceiling(sorted.Count * percentile) - 1;
        index = Math.Clamp(index, 0, sorted.Count - 1);
        return sorted[index];
    }
    
    private sealed record PendingRequest(string Name, long StartTimestamp);
}

public sealed class LatencySnapshot
{
    public static readonly LatencySnapshot Empty = new();

    public string Name { get; init; } = string.Empty;
    public int Count { get; init; }
    public double Avg { get; init; }
    public long Min { get; init; }
    public long Max { get; init; }
    public long P95 { get; init; }
    public long P99 { get; init; }
}