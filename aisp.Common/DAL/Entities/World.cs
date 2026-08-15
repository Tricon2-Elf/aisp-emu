namespace aisp.Common.DAL.Entities;

public class World
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public ushort Port { get; set; }
}
