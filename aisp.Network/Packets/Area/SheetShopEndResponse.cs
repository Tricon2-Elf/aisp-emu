using aisp.Network;

namespace aisp.Network.Packets.Area;

/// <summary>recv_sheet_shop_end_r (0xAE06, case 0x7E272B): u32 result; 0 closes the window and the editor resumes. No ended push exists for this shop.</summary>
public sealed class SheetShopEndResponse(uint result = 0) : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(result);
        return writer.ToBytes();
    }
}
