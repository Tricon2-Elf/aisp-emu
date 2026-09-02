using aisp.Network;

namespace aisp.Network.Packets.Area;

/// <summary>send_sheet_shop_start (0x46EE, wrapper 0x7A8840): the drama editor's 通販 button. Empty.</summary>
public sealed class SheetShopStartRequest : IIncomingPacket<SheetShopStartRequest>
{
    public static SheetShopStartRequest FromBytes(ReadOnlySpan<byte> data) => new();
}
