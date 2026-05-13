namespace ServerSkills.Monitoring;

public class MoveMetrics
{
    private long _moveRequestCount;
    private long _moveSendCount;

    public void RecordMove(int sendCount)
    {
        Interlocked.Increment(ref _moveRequestCount);
        Interlocked.Add(ref _moveSendCount, sendCount);
    }

    public MoveMetricSnapshot SnapshotAndClear()
    {
        long requests = Interlocked.Exchange(ref _moveRequestCount, 0);
        long sends = Interlocked.Exchange(ref _moveSendCount, 0);

        return new MoveMetricSnapshot
        {
            MoveRequests = requests,
            MoveSends = sends,
            AvgRecipients = requests == 0 ? 0 : (double)sends / requests,
        };
    }
}

public class MoveMetricSnapshot
{
    public long MoveRequests { get; init; }
    public long MoveSends { get; init; }
    public double AvgRecipients { get; init; }
}