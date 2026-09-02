using aisp.Network;

namespace aisp.Network.Packets.Area;

/// <summary>send_sheet_shop_end (0xAF54, wrapper 0x7A89A0): the sheet shop window's close button. Empty.</summary>
public sealed class SheetShopEndRequest : IIncomingPacket<SheetShopEndRequest>
{
    public static SheetShopEndRequest FromBytes(ReadOnlySpan<byte> data) => new();
}
