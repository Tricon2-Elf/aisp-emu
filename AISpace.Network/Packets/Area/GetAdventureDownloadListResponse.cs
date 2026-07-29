using AISpace.Network;

namespace AISpace.Network.Packets.Area;

/// <summary>
/// recv_get_adventure_download_list_r begins with a result and entry count.
/// An empty successful list is exactly eight bytes.
/// </summary>
public sealed class GetAdventureDownloadListResponse(uint result = 0) : IOutgoingPacket
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
