namespace AISpace.Common.DAL.Entities;

public class GameChannel
{
    public int Id { get; set; }
    public int ChannelNum { get; set; }
    public ushort Port { get; set; }
    public string IP { get; set; } = string.Empty;
}
