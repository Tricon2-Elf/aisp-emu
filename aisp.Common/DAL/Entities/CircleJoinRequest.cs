namespace aisp.Common.DAL.Entities;

public enum CircleJoinRequestStatus : byte
{
    Pending = 0,
    Accepted = 1,
    Rejected = 2,
    Cancelled = 3,
}

public class CircleJoinRequest
{
    public int Id { get; set; }
    public int CircleId { get; set; }
    public Circle Circle { get; set; } = default!;

    public int RequesterCharacterId { get; set; }
    public Character RequesterCharacter { get; set; } = default!;

    public int TargetCharacterId { get; set; }
    public Character TargetCharacter { get; set; } = default!;

    public CircleJoinRequestStatus Status { get; set; } = CircleJoinRequestStatus.Pending;
    public DateTime CreatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
}
