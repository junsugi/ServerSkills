using System.Collections.Concurrent;

namespace ServerSkills.Monitoring;

public class PacketProfiler
{
    private readonly ConcurrentDictionary<string, ConcurrentQueue<long>> _samples
        = new();

    public void Record(string packetName, long elapsedMs)
    {
        ConcurrentQueue<long> queue =
            _samples.GetOrAdd(packetName, _ => new ConcurrentQueue<long>());

        queue.Enqueue(elapsedMs);
    }

    public PacketProfileSnapshot SnapshotAndClear(string packetName)
    {
        if (!_samples.TryGetValue(packetName, out var queue))
            return PacketProfileSnapshot.Empty;

        List<long> values = new();

        while (queue.TryDequeue(out long value))
        {
            values.Add(value);
        }

        if (values.Count == 0)
            return PacketProfileSnapshot.Empty;

        values.Sort();

        return new PacketProfileSnapshot
        {
            PacketName = packetName,
            Count = values.Count,
            Avg = values.Average(),
            Min = values[0],
            Max = values[^1],
            P95 = Percentile(values, 0.95),
            P99 = Percentile(values, 0.99)
        };
    }

    private static long Percentile(List<long> sorted, double percentile)
    {
        int index =
            (int)Math.Ceiling(sorted.Count * percentile) - 1;

        index = Math.Clamp(index, 0, sorted.Count - 1);

        return sorted[index];
    }
}

public sealed class PacketProfileSnapshot
{
    public static readonly PacketProfileSnapshot Empty = new();
    public string PacketName { get; init; } = string.Empty;
    public int Count { get; init; }
    public double Avg { get; init; }
    public long Min { get; init; }
    public long Max { get; init; }
    public long P95 { get; init; }
    public long P99 { get; init; }
}