using Google.Protobuf.Protocol;

namespace ServerSkills;

public enum PickItemRequestState
{
    Pending,
    Completed,
}

public class PickItemRequestRecord
{
    public int ObjectId { get; init; }
    public PickItemRequestState State { get; set; }
    public ResultCode ResultCode { get; set; }
    public ItemInfo? ItemInfo { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
}