using System.Collections.Concurrent;
using System.Diagnostics;

namespace ServerSkills.Monitoring;

public sealed class LatencyTracker
{
    private readonly ConcurrentDictionary<int, long> _pendingRequests = new();
    private readonly ConcurrentQueue<long> _samples = new();

    public void Start(int requestId)
    {
        _pendingRequests[requestId] = Stopwatch.GetTimestamp();
    }

    public bool Complete(int requestId)
    {
        if (!_pendingRequests.TryRemove(requestId, out long start))
            return false;

        long end = Stopwatch.GetTimestamp();
        long elapsedMs = (end - start) * 1000 / Stopwatch.Frequency;

        _samples.Enqueue(elapsedMs);
        return true;
    }
    
    public LatencySnapshot SnapshotAndClear()
    {
        List<long> samples = new();

        while (_samples.TryDequeue(out long elapsedMs))
        {
            samples.Add(elapsedMs);
        }

        if (samples.Count == 0)
            return LatencySnapshot.Empty;

        samples.Sort();

        return new LatencySnapshot
        {
            Count = samples.Count,
            Avg = samples.Average(),
            Min = samples[0],
            Max = samples[^1],
            P95 = Percentile(samples, 0.95),
            P99 = Percentile(samples, 0.99)
        };
    }
    
    private static long Percentile(List<long> sorted, double percentile)
    {
        int index = (int)Math.Ceiling(sorted.Count * percentile) - 1;
        index = Math.Clamp(index, 0, sorted.Count - 1);
        return sorted[index];
    }
}

public sealed class LatencySnapshot
{
    public static readonly LatencySnapshot Empty = new();

    public int Count { get; init; }
    public double Avg { get; init; }
    public long Min { get; init; }
    public long Max { get; init; }
    public long P95 { get; init; }
    public long P99 { get; init; }
}