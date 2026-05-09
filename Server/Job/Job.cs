namespace ServerSkills.Job;

public abstract class IJob
{
    public abstract void Execute();
    public bool Cancel { get; set; } = false;
}

public class Job : IJob
{
    private Action _action;

    public Job(Action action)
    {
        _action = action;
    }
    
    public override void Execute()
    {
        if (Cancel == false)
            _action.Invoke();
    }
}

public class Job<T1> : IJob
{
    Action<T1> _action;
    T1 _t1;

    public Job(Action<T1> action, T1 t1)
    {
        _action = action;
        _t1 = t1;
    }

    public override void Execute()
    {
        if (Cancel == false)
            _action.Invoke(_t1);
    }
}

public class Job<T1, T2> : IJob
{
    Action<T1, T2> _action;
    T1 _t1;
    T2 _t2;

    public Job(Action<T1, T2> action, T1 t1, T2 t2)
    {
        _action = action;
        _t1 = t1;
        _t2 = t2;
    }

    public override void Execute()
    {
        if (Cancel == false)
            _action.Invoke(_t1, _t2);
    }
}