using aisp.Network;

namespace aisp.Network.Packets.Area;

/// <summary>recv_notify_update_nameplate (0x64AD, case 0x7D11CF): u32 object id, u32 name plate variant; changes a character's plate live.</summary>
public sealed class NotifyUpdateNameplate(uint objectId, uint namePlate) : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(objectId);
        writer.Write(namePlate);
        return writer.ToBytes();
    }
}
