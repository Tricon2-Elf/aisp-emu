using aisp.Network;

namespace aisp.Network.Packets.Area;

/// <summary>
/// recv_sheet_shop_start_r (0x6F5C, case 0x7D3B93), 12 bytes: u32 result, int64 price of one 原稿用紙 in デレ.
/// Result 0 opens the sheet shop window over the editor with that unit price; the remaining-sheet label comes
/// from the last recv_adventure_updated_sheet_stack and the balance from the money manager, so nothing else is
/// needed. Non-zero leaves the editor where it was.
/// </summary>
public sealed class SheetShopStartResponse(uint result, long sheetPriceAi) : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(result);
        writer.Write((ulong)sheetPriceAi);
        return writer.ToBytes();
    }
}
