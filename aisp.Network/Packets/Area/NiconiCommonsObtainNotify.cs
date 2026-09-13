using aisp.Network;

namespace aisp.Network.Packets.Area;

/// <summary>
/// recv_niconi_commons_obtain (0xB2AE). Parser 0x7E3DB0: one u32 (niconi_commonsid=), remaining empty.
/// Pushed after a successful commons buy so the Have tab updates.
/// </summary>
public sealed class NiconiCommonsObtainNotify(uint id) : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(id);
        return writer.ToBytes();
    }
}
