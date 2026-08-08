namespace AISpace.Common.DAL.Entities;

public class Circle
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public uint Status { get; set; } = 1;
    public uint MarkId { get; set; }
    public string Mark { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string MessageDate { get; set; } = string.Empty;
    public int LeaderCharacterId { get; set; }
    public Character LeaderCharacter { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<CircleMember> Members { get; set; } = [];
    public ICollection<CircleJoinRequest> JoinRequests { get; set; } = [];
}
