using aisp.Network;

namespace aisp.Network.Packets.Area;

/// <summary>
/// send_sheet_shop_buy (0x1E92, wrapper 0x7A8B00), 12 bytes: u32 sheet count, int64 unit price as the window
/// shows it (not the total). The window already refused a zero count and a total above the デレ balance.
/// </summary>
public sealed class SheetShopBuyRequest(uint sheetCount, long sheetPriceAi)
    : IIncomingPacket<SheetShopBuyRequest>
{
    public uint SheetCount { get; } = sheetCount;
    public long SheetPriceAi { get; } = sheetPriceAi;

    public static SheetShopBuyRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        return new SheetShopBuyRequest(reader.ReadUInt(), (long)reader.ReadULong());
    }
}
