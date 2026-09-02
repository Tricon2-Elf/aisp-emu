using aisp.Network;

namespace aisp.Network.Packets.Area;

/// <summary>
/// The 12-byte u32 result + int64 scriptId reply shared by recv_adventure_upload_delete_request_r (0xFEF7),
/// recv_adventure_download_delete_request_r (0x35CA) and recv_adventure_shop_remove_buy_history_r (0x1915).
/// On result 0 the client drops the entry from the matching local list.
/// </summary>
public sealed class AdventureScriptIdResponse(uint result, long scriptId) : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(result);
        writer.Write((ulong)scriptId);
        return writer.ToBytes();
    }
}
