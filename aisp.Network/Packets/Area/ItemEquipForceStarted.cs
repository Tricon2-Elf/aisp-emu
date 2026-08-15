using aisp.Network;

namespace aisp.Network.Packets.Area;

public class ItemEquipForceStarted(uint objId) : IOutgoingPacket
{
    public uint ObjId = objId;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(ObjId);
        return writer.ToBytes();
    }
}
