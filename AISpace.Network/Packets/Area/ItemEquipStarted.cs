using AISpace.Network;

namespace AISpace.Network.Packets.Area;

public class ItemEquipStarted(uint objId) : IOutgoingPacket
{
    public uint ObjId = objId;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(ObjId);
        return writer.ToBytes();
    }
}
