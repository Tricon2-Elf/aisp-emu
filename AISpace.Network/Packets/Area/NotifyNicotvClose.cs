namespace AISpace.Network.Packets.Area;

public sealed class NotifyNicotvClose(uint nicotvId) : IOutgoingPacket
{
    public uint NicotvId { get; } = nicotvId;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(NicotvId);
        return writer.ToBytes();
    }
}
