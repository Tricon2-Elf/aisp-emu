using aisp.Network;

namespace aisp.Network.Packets.Area;

/// <summary>recv_adventure_shop_end_r (0xC605), 4 bytes: UInt Result (0 = ok). The window stays open until this arrives.</summary>
public sealed class AdventureShopEndResponse(uint result = 0) : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(result);
        return writer.ToBytes();
    }
}
