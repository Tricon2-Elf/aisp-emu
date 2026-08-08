namespace AISpace.Common.DAL.Entities;

public class CircleMember
{
    public int CircleId { get; set; }
    public Circle Circle { get; set; } = default!;
    public int CharacterId { get; set; }
    public Character Character { get; set; } = default!;

    public uint AuthLevel { get; set; }
    public DateTime JoinedAt { get; set; }
}
