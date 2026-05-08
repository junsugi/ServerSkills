using ServerCore;

namespace ServerSkills.Job;

struct JobTimerElem : IComparable<JobTimerElem>
{
    public int ExecTick; // 실행 시간
    public IJob Job;

    public int CompareTo(JobTimerElem other)
    {
        return other.ExecTick - ExecTick;
    }
}


public class JobTimer
{
    private PriorityQueue<JobTimerElem> _pq = new();
    private object _lock = new ();

    public void Push(IJob job, int tickAfter = 0)
    {
        JobTimerElem jobElement;
        jobElement.ExecTick = System.Environment.TickCount + tickAfter;
        jobElement.Job = job;

        lock (_lock)
        {
            _pq.Push(jobElement);
        }
    }

    public void Flush()
    {
        while (true)
        {
            int now = System.Environment.TickCount;

            JobTimerElem jobElement;

            lock (_lock)
            {
                if (_pq.Count == 0)
                    break;

                jobElement = _pq.Peek();
                if (jobElement.ExecTick > now)
                    break;

                _pq.Pop();
            }

            jobElement.Job.Execute();
        }
    }
}