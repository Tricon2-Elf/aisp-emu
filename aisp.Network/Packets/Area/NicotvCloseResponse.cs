namespace aisp.Network.Packets.Area;

public sealed class NicotvCloseResponse(uint result, uint nicotvId) : IOutgoingPacket
{
    public uint Result { get; } = result;
    public uint NicotvId { get; } = nicotvId;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(Result);
        writer.Write(NicotvId);
        return writer.ToBytes();
    }
}
