namespace aisp.Network.Packets.Area;

public sealed class NicotvCloseRequest(uint nicotvId) : IIncomingPacket<NicotvCloseRequest>
{
    public const int WireSize = sizeof(uint);

    public uint NicotvId { get; } = nicotvId;

    public static NicotvCloseRequest FromBytes(ReadOnlySpan<byte> data)
    {
        if (data.Length != WireSize)
            throw new InvalidDataException(
                $"{nameof(NicotvCloseRequest)} requires exactly {WireSize} bytes, received {data.Length}."
            );

        return new NicotvCloseRequest(new PacketReader(data).ReadUInt());
    }
}
