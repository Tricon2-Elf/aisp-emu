using aisp.Network.Data;

namespace aisp.Network.Packets.Area;

public sealed class GetFreeFriendLinkTagResponse(
    uint result,
    IReadOnlyList<FriendLinkTagData>? tags = null
) : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(result);
        var count = Math.Min(tags?.Count ?? 0, 100);
        writer.Write((uint)count);
        for (var i = 0; i < count; i++)
            tags![i].Write(writer);
        return writer.ToBytes();
    }
}
