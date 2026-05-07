using System.Collections.Concurrent;
using ServerSkills.Monitoring;

namespace DummyClient.Packet;

public class PendingRequestManager(LatencyTracker latencyTracker)
{
    private int _nextRequestId = 1;
    private readonly ConcurrentDictionary<int, Action> _callbacks = new();

    public int Register(string name, Action onCompleted)
    {
        int requestId = Interlocked.Increment(ref _nextRequestId);

        latencyTracker.Start(requestId, name);
        _callbacks[requestId] = onCompleted;
        
        return requestId;
    }

    public void Completed(int requestId)
    {
        latencyTracker.Complete(requestId);
        
        if (_callbacks.TryRemove(requestId, out var callback))
            callback.Invoke();
    }
}