using aisp.Network;

namespace aisp.Network.Packets.Area;

/// <summary>recv_niconi_commons_shop_end_r (0xAA13): UInt result (0 = ok).</summary>
public sealed class NiconiCommonsShopEndResponse(uint result = 0) : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(result);
        return writer.ToBytes();
    }
}
