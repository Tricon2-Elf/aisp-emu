namespace AISpace.Common.DAL.Entities;

public class Shop
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public long BannerVisualId { get; set; }
    public bool IsEnabled { get; set; } = true;

    public ICollection<ShopItem> Items { get; set; } = new List<ShopItem>();
    public ICollection<Npc> Npcs { get; set; } = new List<Npc>();
}
