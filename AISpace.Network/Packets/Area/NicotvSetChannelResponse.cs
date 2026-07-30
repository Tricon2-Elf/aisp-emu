namespace AISpace.Network.Packets.Area;

public sealed class NicotvSetChannelResponse(uint nicotvId, uint channelId) : IOutgoingPacket
{
    public uint NicotvId { get; } = nicotvId;
    public uint ChannelId { get; } = channelId;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(NicotvId);
        writer.Write(ChannelId);
        return writer.ToBytes();
    }
}
