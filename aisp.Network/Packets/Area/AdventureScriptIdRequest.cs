using aisp.Network;

namespace aisp.Network.Packets.Area;

/// <summary>
/// The int64-scriptId request body shared by send_adventure_upload_delete_request (0xCB22), send_adventure_download_delete_request
/// (0x628C) and send_adventure_shop_remove_buy_history (0x454B).
/// </summary>
public sealed class AdventureScriptIdRequest(long scriptId)
    : IIncomingPacket<AdventureScriptIdRequest>
{
    public long ScriptId { get; } = scriptId;

    public static AdventureScriptIdRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        return new AdventureScriptIdRequest((long)reader.ReadULong());
    }
}
