using aisp.Network;

namespace aisp.Network.Packets.Area;

/// <summary>recv_adventure_end_r (0xC1D1), 4 bytes: UInt Result (0 = ok).</summary>
public sealed class AdventureEndResponse(uint result = 0) : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(result);
        return writer.ToBytes();
    }
}
