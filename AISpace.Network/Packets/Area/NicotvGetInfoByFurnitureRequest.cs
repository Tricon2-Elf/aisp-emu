namespace AISpace.Network.Packets.Area;

public sealed class NicotvGetInfoByFurnitureRequest(uint furnitureId)
    : IIncomingPacket<NicotvGetInfoByFurnitureRequest>
{
    public const int WireSize = sizeof(uint);

    public uint FurnitureId { get; } = furnitureId;

    public static NicotvGetInfoByFurnitureRequest FromBytes(ReadOnlySpan<byte> data)
    {
        if (data.Length != WireSize)
            throw new InvalidDataException(
                $"{nameof(NicotvGetInfoByFurnitureRequest)} requires exactly {WireSize} bytes, received {data.Length}."
            );

        return new NicotvGetInfoByFurnitureRequest(new PacketReader(data).ReadUInt());
    }
}
