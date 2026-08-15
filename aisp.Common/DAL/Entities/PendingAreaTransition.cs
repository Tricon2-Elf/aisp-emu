namespace aisp.Common.DAL.Entities;

public class PendingMapTransfer
{
    public int UserId { get; set; }
    public uint MapId { get; set; }
    public uint MyRoomId { get; set; }
    public int ChannelId { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
    public int Rotation { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
}
