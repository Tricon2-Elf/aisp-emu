using aisp.Network;

namespace aisp.Network.Packets.Area;

/// <summary>send_adventure_shop_download_request (0x9F15, wrapper 0x7A7F40): int64 scriptId. Re-download of a 購入履歴 entry.</summary>
public sealed class AdventureShopDownloadRequestRequest(long scriptId)
    : IIncomingPacket<AdventureShopDownloadRequestRequest>
{
    public long ScriptId { get; } = scriptId;

    public static AdventureShopDownloadRequestRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        return new AdventureShopDownloadRequestRequest((long)reader.ReadULong());
    }
}
