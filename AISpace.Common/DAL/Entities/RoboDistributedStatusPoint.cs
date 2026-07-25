namespace AISpace.Common.DAL.Entities;

public sealed class RoboDistributedStatusPoint
{
    public int CharacterId { get; set; }
    public uint RoboId { get; set; }
    public byte StatusIndex { get; set; }
    public uint Value { get; set; }
    public Robo Robo { get; set; } = default!;
}
