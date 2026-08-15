using aisp.Network;

namespace aisp.Network.Packets.Area;

public class ItemUpdateListNotify(uint place, uint serialId, uint targetId) : IOutgoingPacket
{
    public uint Place { get; } = place;
    public uint SerialId { get; } = serialId;
    public uint TargetId { get; } = targetId;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(Place);
        writer.Write(SerialId);
        writer.Write(TargetId);
        return writer.ToBytes();
    }
}
