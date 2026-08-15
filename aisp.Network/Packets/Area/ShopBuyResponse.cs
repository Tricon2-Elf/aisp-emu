using aisp.Network;

namespace aisp.Network.Packets.Area;

public sealed class ShopBuyResponse(uint result, ulong remained) : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(result);
        writer.Write(remained);
        return writer.ToBytes();
    }
}
