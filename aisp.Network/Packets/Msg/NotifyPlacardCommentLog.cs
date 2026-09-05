namespace aisp.Network.Packets.Msg;

/// <summary>Delivers the placard's comment history after its request is acknowledged.</summary>
public sealed class NotifyPlacardCommentLog(
    uint result,
    uint placardId,
    IReadOnlyList<PlacardCommentLogEntry> comments
) : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(result);
        writer.Write(placardId);
        var count = Math.Min(comments.Count, 100);
        writer.Write((uint)count);
        for (var i = 0; i < count; i++)
            comments[i].Write(writer);
        return writer.ToBytes();
    }
}
