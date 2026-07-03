namespace AISpace.Common.DAL.Entities;

public class ShopItem
{
    public int Id { get; set; }
    public int ShopId { get; set; }
    public int ItemId { get; set; }
    public long AiPrice { get; set; }
    public long NicoPrice { get; set; }
    public int SortOrder { get; set; }
    public bool IsEnabled { get; set; } = true;

    public Shop Shop { get; set; } = default!;
}
