namespace AISpace.Network.Packets.Area;

public sealed class NicotvGetPlayheadTimeRequestRRequest(
    uint nicotvId,
    uint requestingUserId,
    uint seconds
) : IIncomingPacket<NicotvGetPlayheadTimeRequestRRequest>
{
    public const int WireSize = sizeof(uint) * 3;

    public uint NicotvId { get; } = nicotvId;
    public uint RequestingUserId { get; } = requestingUserId;
    public uint Seconds { get; } = seconds;

    public static NicotvGetPlayheadTimeRequestRRequest FromBytes(ReadOnlySpan<byte> data)
    {
        if (data.Length != WireSize)
            throw new InvalidDataException(
                $"{nameof(NicotvGetPlayheadTimeRequestRRequest)} requires exactly {WireSize} bytes, received {data.Length}."
            );

        var reader = new PacketReader(data);
        return new NicotvGetPlayheadTimeRequestRRequest(
            reader.ReadUInt(),
            reader.ReadUInt(),
            reader.ReadUInt()
        );
    }
}
