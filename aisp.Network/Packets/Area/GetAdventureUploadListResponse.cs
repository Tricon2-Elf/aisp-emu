using aisp.Network;

namespace aisp.Network.Packets.Area;

/// <summary>
/// recv_get_adventure_upload_list_r contains a result, a count (max 100) and count 0x630-byte records.
/// This response represents an empty list.
/// </summary>
public sealed class GetAdventureUploadListResponse(uint result = 0) : IOutgoingPacket
{
    public uint Result { get; } = result;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(Result);
        writer.Write(0u);
        return writer.ToBytes();
    }
}
