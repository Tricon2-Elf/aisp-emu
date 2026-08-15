namespace aisp.Common.DAL.Entities;

public class GameChannel
{
    public int Id { get; set; }
    public int ChannelNum { get; set; }
    public ushort Port { get; set; }
    public string IP { get; set; } = string.Empty;
    public uint CurrentUsers { get; set; }
    public uint MaxUsers { get; set; } = 1000;
    public uint MapId { get; set; }
}
