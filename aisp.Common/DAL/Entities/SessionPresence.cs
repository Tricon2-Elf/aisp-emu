using aisp.Common;

namespace aisp.Common.DAL.Entities;

public class SessionPresence
{
    public Guid ConnectionId { get; set; }
    public ServerType ServerType { get; set; }
    public int UserId { get; set; }
    public uint CharacterId { get; set; }
    public uint MapId { get; set; }
    public int ChannelId { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
    public int Rotation { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
