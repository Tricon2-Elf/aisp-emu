namespace AISpace.Common.DAL.Entities;

public sealed class RoboItemUseEffect
{
    public int CharacterId { get; set; }
    public uint RoboId { get; set; }
    public byte SlotIndex { get; set; }
    public uint ItemSerialId { get; set; }
    public uint Enabled { get; set; }
    public uint ItemDefinitionId { get; set; }
    public uint EffectType { get; set; }
    public uint Parameter0 { get; set; }
    public uint Parameter1 { get; set; }
    public uint Parameter2 { get; set; }
    public uint Parameter3 { get; set; }
    public uint Parameter4 { get; set; }
    public byte OverwriteExisting { get; set; }
    public Robo Robo { get; set; } = default!;
}
