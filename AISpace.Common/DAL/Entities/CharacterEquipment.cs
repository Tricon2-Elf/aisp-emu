namespace AISpace.Common.DAL.Entities;

public class CharacterEquipment
{
    public int CharacterId { get; set; }
    public Character Character { get; set; } = default!;

    public byte SlotIndex { get; set; } // 0..29

    public int ItemId { get; set; }
    public Item Item { get; set; } = default!;
}