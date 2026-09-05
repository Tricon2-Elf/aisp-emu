using aisp.Network;

namespace aisp.Network.Packets.Area;

/// <summary>recv_user_status_update_r (0xD824, case 0x7EDB94), 8 bytes: u32 result, u32 object id.</summary>
public sealed class UserStatusUpdateResponse(uint result, uint objectId) : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(result);
        writer.Write(objectId);
        return writer.ToBytes();
    }
}
