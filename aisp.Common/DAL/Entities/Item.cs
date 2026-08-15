namespace aisp.Common.DAL.Entities;

public class Item
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Socket { get; set; }
    public int IconId { get; set; } = 1;

    /// <summary>Persisted wardrobe/catalog category. Null means derive from canonical name and item id.</summary>
    public int? CatalogCategory { get; set; }
    public Furniture? Furniture { get; set; }
}
