using aisp.Network;

namespace aisp.Network.Packets.Area;

/// <summary>
/// send_niconi_commons_shop_buy_commons (0x7438). Wrapper 0x7AB080, alloc 10:
/// UInt id + extra byte (same shape as buy_figure).
/// </summary>
public sealed class NiconiCommonsShopBuyCommonsRequest
    : IIncomingPacket<NiconiCommonsShopBuyCommonsRequest>
{
    public uint Id { get; init; }
    public byte Extra { get; init; }

    public static NiconiCommonsShopBuyCommonsRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        var id = reader.ReadUInt();
        var extra = data.Length > 4 ? reader.ReadByte() : (byte)0;
        return new NiconiCommonsShopBuyCommonsRequest { Id = id, Extra = extra };
    }
}
