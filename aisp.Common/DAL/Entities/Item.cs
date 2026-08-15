namespace aisp.Common.DAL.Entities;

public class Item
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Socket { get; set; }
    public int IconId { get; set; } = 1;
    public Furniture? Furniture { get; set; }
}
