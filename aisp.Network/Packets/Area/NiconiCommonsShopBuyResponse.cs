using aisp.Network;

namespace aisp.Network.Packets.Area;

/// <summary>recv_niconi_commons_shop_buy_r (0x96BE): UInt result, UInt64 remained デレ. Parser 0x7DDB50.</summary>
public sealed class NiconiCommonsShopBuyResponse(uint result, ulong remained) : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(result);
        writer.Write(remained);
        return writer.ToBytes();
    }
}
