namespace aisp.Network.Packets.Area;

public sealed class NicotvGetPlayheadTimeRequest(uint nicotvId)
    : IIncomingPacket<NicotvGetPlayheadTimeRequest>
{
    public const int WireSize = sizeof(uint);

    public uint NicotvId { get; } = nicotvId;

    public static NicotvGetPlayheadTimeRequest FromBytes(ReadOnlySpan<byte> data)
    {
        if (data.Length != WireSize)
            throw new InvalidDataException(
                $"{nameof(NicotvGetPlayheadTimeRequest)} requires exactly {WireSize} bytes, received {data.Length}."
            );

        return new NicotvGetPlayheadTimeRequest(new PacketReader(data).ReadUInt());
    }
}
