namespace AISpace.Common.DAL.Entities;

public class GameChannel
{
    public int Id { get; set; }           // Primary key

    public int ChannelNum { get; set; } //Between 0-9
    public ushort Port { get; set; }
    public string IP { get; set; } = string.Empty;
}
