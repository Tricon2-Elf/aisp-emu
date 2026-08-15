using aisp.Network.Data;

namespace aisp.Network.Packets.Area;

public sealed class NicotvOpenByFurnitureRequest(uint furnitureId, NicotvData nicotv)
    : IIncomingPacket<NicotvOpenByFurnitureRequest>
{
    public const int WireSize = sizeof(uint) + NicotvData.WireSize;

    public uint FurnitureId { get; } = furnitureId;
    public NicotvData Nicotv { get; } = nicotv;

    public static NicotvOpenByFurnitureRequest FromBytes(ReadOnlySpan<byte> data)
    {
        if (data.Length != WireSize)
            throw new InvalidDataException(
                $"{nameof(NicotvOpenByFurnitureRequest)} requires exactly {WireSize} bytes, received {data.Length}."
            );

        return new NicotvOpenByFurnitureRequest(
            new PacketReader(data).ReadUInt(),
            NicotvData.FromBytes(data[sizeof(uint)..])
        );
    }
}
