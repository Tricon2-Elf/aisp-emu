using aisp.Network;

namespace aisp.Network.Packets.Area;

/// <summary>recv_adventure_start_r (0x7C69), 4 bytes: UInt Result (0 = ok).</summary>
public sealed class AdventureStartResponse(uint result = 0) : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(result);
        return writer.ToBytes();
    }
}
