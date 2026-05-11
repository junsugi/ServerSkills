namespace ServerSkills;

public class TestManager
{
    public static readonly TestManager Instance = new();

    private int _targetPlayerCount = 100;
    private int _playerCount; 
    
    private TestManager()
    {}

    public void IncreasePlayer()
    {
        // HashSet으로 중복제거?
        Interlocked.Increment(ref _playerCount);
    }

    public bool IsDoneTargetPlayer()
    {
        return _playerCount == _targetPlayerCount;
    }
}