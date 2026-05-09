namespace ServerSkills.Job;

public sealed record JobMetrics(long EnqueueAt, long StartAt, long EndAt);