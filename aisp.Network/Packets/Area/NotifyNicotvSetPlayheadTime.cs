namespace aisp.Network.Packets.Area;

/// <summary>recv_notify_nicotv_set_playhead_time (0xAAAE): nicotvid + seconds.</summary>
public sealed class NotifyNicotvSetPlayheadTime(uint nicotvId, uint seconds) : IOutgoingPacket
{
    public uint NicotvId { get; } = nicotvId;
    public uint Seconds { get; } = seconds;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(NicotvId);
        writer.Write(Seconds);
        return writer.ToBytes();
    }
}
