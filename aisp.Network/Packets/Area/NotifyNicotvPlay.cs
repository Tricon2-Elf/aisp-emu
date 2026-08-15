namespace aisp.Network.Packets.Area;

/// <summary>recv_notify_nicotv_play (0x8A86): nicotvid + status.</summary>
public sealed class NotifyNicotvPlay(uint nicotvId, uint status) : IOutgoingPacket
{
    public uint NicotvId { get; } = nicotvId;
    public uint Status { get; } = status;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(NicotvId);
        writer.Write(Status);
        return writer.ToBytes();
    }
}
