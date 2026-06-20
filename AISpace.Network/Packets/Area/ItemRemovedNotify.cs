using AISpace.Network;

namespace AISpace.Network.Packets.Area;

public class ItemRemovedNotify(uint objId, uint serialId, uint socketBit) : IOutgoingPacket
{
    public uint ObjId { get; } = objId;
    public uint SerialId { get; } = serialId;
    public uint SocketBit { get; } = socketBit;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(ObjId);
        writer.Write(SerialId);
        writer.Write(SocketBit);
        return writer.ToBytes();
    }
}
