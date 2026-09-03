using aisp.Network;
using aisp.Network.Data;

namespace aisp.Network.Packets.Area;

/// <summary>recv_notify_user_status_update (0x7016, case 0x7D39CF), 57 bytes: u32 object id, then the 53-byte status record; sent to everyone on the map, the owner included.</summary>
public sealed class NotifyUserStatusUpdate(uint objectId, UserStatusData status) : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(objectId);
        writer.Write(status.ToBytes());
        return writer.ToBytes();
    }
}
