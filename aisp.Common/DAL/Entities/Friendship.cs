namespace aisp.Common.DAL.Entities;

public enum FriendRequestStatus : byte
{
    Pending = 0,
    Accepted = 1,
    Rejected = 2,
}

public sealed class Friendship
{
    public int CharacterIdLow { get; set; }
    public Character CharacterLow { get; set; } = default!;
    public int CharacterIdHigh { get; set; }
    public Character CharacterHigh { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
}

public sealed class FriendRequest
{
    public int Id { get; set; }
    public int RequesterCharacterId { get; set; }
    public Character RequesterCharacter { get; set; } = default!;
    public int TargetCharacterId { get; set; }
    public Character TargetCharacter { get; set; } = default!;
    public FriendRequestStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
}
