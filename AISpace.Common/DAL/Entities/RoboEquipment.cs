namespace AISpace.Common.DAL.Entities;

public sealed class RoboEquipment
{
    public int CharacterId { get; set; }
    public uint RoboId { get; set; }
    public byte SlotIndex { get; set; }
    public uint ItemId { get; set; }
    public uint Socket { get; set; }
    public Robo Robo { get; set; } = default!;
}
