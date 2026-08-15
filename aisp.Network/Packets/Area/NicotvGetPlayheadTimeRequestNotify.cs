namespace aisp.Network.Packets.Area;

public sealed class NicotvGetPlayheadTimeRequestNotify(uint nicotvId, uint requestingUserId)
    : IOutgoingPacket
{
    public uint NicotvId { get; } = nicotvId;
    public uint RequestingUserId { get; } = requestingUserId;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(NicotvId);
        writer.Write(RequestingUserId);
        return writer.ToBytes();
    }
}
