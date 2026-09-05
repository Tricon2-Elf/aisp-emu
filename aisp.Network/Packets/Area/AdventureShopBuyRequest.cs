using aisp.Network;

namespace aisp.Network.Packets.Area;

/// <summary>
/// send_adventure_shop_buy (0x0289, wrapper 0x7A7D00): int64 scriptId, int64 price, u8 price type. The window
/// sends the listing's first price with type 0 (デレ / AI points, checked against that purse client-side) and
/// only falls back to the second price with type 1 (ニコニコポイント) when the first is 0.
/// </summary>
public sealed class AdventureShopBuyRequest(long scriptId, long price, byte priceType)
    : IIncomingPacket<AdventureShopBuyRequest>
{
    public const byte AiPointsPriceType = 0;

    public long ScriptId { get; } = scriptId;
    public long Price { get; } = price;
    public byte PriceType { get; } = priceType;

    public static AdventureShopBuyRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        return new AdventureShopBuyRequest(
            (long)reader.ReadULong(),
            (long)reader.ReadULong(),
            reader.ReadByte()
        );
    }
}
