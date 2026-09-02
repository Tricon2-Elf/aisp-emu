using aisp.Network;

namespace aisp.Network.Packets.Area;

/// <summary>
/// recv_adventure_shop_download_request_r (0x46BC, case 0x7CCB89), fixed 53 bytes: u32 result, int64 scriptId,
/// char[41] one-time ticket. On result 0 the client POSTs userid / scriptid / ticket to download.php right inside
/// the handler, stores the reply as dl/drama/ai{scriptId}.txt, and adds the disc to its download list and (if
/// missing) its purchase history. Pushed by the server after a purchase as well, since the client never asks.
/// </summary>
public sealed class AdventureShopDownloadRequestResponse(
    uint result,
    long scriptId,
    string ticket = ""
) : IOutgoingPacket
{
    public const int TicketLength = 41;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(result);
        writer.Write((ulong)scriptId);
        writer.WriteFixedString(ticket, TicketLength);
        return writer.ToBytes();
    }
}
