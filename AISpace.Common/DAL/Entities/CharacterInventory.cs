namespace AISpace.Common.DAL.Entities;

public class CharacterInventory
{
    public int CharacterId { get; set; }
    public Character Character { get; set; } = default!;

    public int ItemId { get; set; }
    public Item Item { get; set; } = default!;

    public int Quantity { get; set; }
}
