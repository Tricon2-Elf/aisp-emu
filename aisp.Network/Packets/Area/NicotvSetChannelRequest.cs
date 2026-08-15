namespace aisp.Network.Packets.Area;

public sealed class NicotvSetChannelRequest(uint nicotvId, uint channelId)
    : IIncomingPacket<NicotvSetChannelRequest>
{
    public const int WireSize = sizeof(uint) * 2;

    public uint NicotvId { get; } = nicotvId;
    public uint ChannelId { get; } = channelId;

    public static NicotvSetChannelRequest FromBytes(ReadOnlySpan<byte> data)
    {
        if (data.Length != WireSize)
            throw new InvalidDataException(
                $"{nameof(NicotvSetChannelRequest)} requires exactly {WireSize} bytes, received {data.Length}."
            );

        var reader = new PacketReader(data);
        return new NicotvSetChannelRequest(reader.ReadUInt(), reader.ReadUInt());
    }
}
