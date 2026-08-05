namespace AISpace.Network.Packets.Area;

/// <summary>recv_notify_nicotv_set_comment_visible (0xE192): nicotvid + visible.</summary>
public sealed class NotifyNicotvSetCommentVisible(uint nicotvId, uint visible) : IOutgoingPacket
{
    public uint NicotvId { get; } = nicotvId;
    public uint Visible { get; } = visible;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(NicotvId);
        writer.Write(Visible);
        return writer.ToBytes();
    }
}
