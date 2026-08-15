namespace aisp.Network.Packets.Area;

public sealed class NicotvGetPlayheadTimeResponse(uint nicotvId, uint seconds) : IOutgoingPacket
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
