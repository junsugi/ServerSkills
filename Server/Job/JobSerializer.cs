namespace ServerSkills.Job;

public class JobSerializer
{
    private JobTimer _timer = new();
    private Queue<IJob> _jobQueue = new();
    private object _lock = new();
    private bool _flush = false;
    
    public void Push(Action action) { Push(new Job(action)); }
    public void Push<T1>(Action<T1> action, T1 t1) { Push(new Job<T1>(action, t1)); }
    
    public void Push(IJob job)
    {
        lock (_lock)
        {
            _jobQueue.Enqueue(job);
        }
    }
    
    public void Flush()
    {
        _timer.Flush();

        while (true)
        {
            IJob? job = Pop();
            if (job == null)
                return;

            job.Execute();
        }
    }
    
    private IJob? Pop()
    {
        lock (_lock)
        {
            if (_jobQueue.Count == 0)
            {
                _flush = false;
                return null;
            }
            return _jobQueue.Dequeue();
        }
    }
}