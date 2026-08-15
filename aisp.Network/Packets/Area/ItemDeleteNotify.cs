using aisp.Network;

namespace aisp.Network.Packets.Area;

public sealed class ItemDeleteNotify(uint place, uint serialId) : IOutgoingPacket
{
    public uint Place { get; } = place;
    public uint SerialId { get; } = serialId;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(Place);
        writer.Write(SerialId);
        return writer.ToBytes();
    }
}
