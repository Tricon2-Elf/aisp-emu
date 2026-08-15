using aisp.Network.Data;

namespace aisp.Network.Packets.Area;

public sealed class NicotvOpenResponse(uint furnitureId, uint nicotvId, NicotvData nicotv)
    : IOutgoingPacket
{
    public uint FurnitureId { get; } = furnitureId;
    public uint NicotvId { get; } = nicotvId;
    public NicotvData Nicotv { get; } = nicotv;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(FurnitureId);
        writer.Write(NicotvId);
        writer.Write(Nicotv.ToBytes());
        return writer.ToBytes();
    }
}
