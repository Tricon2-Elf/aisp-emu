using aisp.Network;

namespace aisp.Network.Packets.Area;

/// <summary>recv_adventure_updated_sheet_stack (0xABE0), 4 bytes: UInt SheetStock — the account's 原稿用紙 count.</summary>
public sealed class AdventureUpdatedSheetStackNotify(uint sheetStock) : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(sheetStock);
        return writer.ToBytes();
    }
}
