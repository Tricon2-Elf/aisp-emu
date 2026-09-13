using aisp.Network;

namespace aisp.Network.Packets.Area;

/// <summary>
/// send_niconi_commons_shop_buy_figure (0x78DF). Wrapper 0x7AB260, alloc 10:
/// UInt figure registry id from the shop_item row + 1 extra byte.
/// </summary>
public sealed class NiconiCommonsShopBuyFigureRequest
    : IIncomingPacket<NiconiCommonsShopBuyFigureRequest>
{
    public uint FigureId { get; init; }
    public byte Extra { get; init; }

    public static NiconiCommonsShopBuyFigureRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        var figureId = reader.ReadUInt();
        var extra = data.Length > 4 ? reader.ReadByte() : (byte)0;
        return new NiconiCommonsShopBuyFigureRequest { FigureId = figureId, Extra = extra };
    }
}
