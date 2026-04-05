using AISpace.Network;

namespace AISpace.Network.Packets.Area;

public class ItemTryEquipped(uint objId, uint serialId, uint socketBit) : IOutgoingPacket
{
    public uint ObjId = objId;
    public uint SerialId = serialId;
    public uint SocketBit = socketBit;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(ObjId);
        writer.Write(SerialId);
        writer.Write(SocketBit);
        return writer.ToBytes();
    }
}
