using AISpace.Network;

namespace AISpace.Network.Packets.Area;

public class MapEnterResponse(uint result, uint mapId, uint objId, float x, float y, float z, float rot) : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(result);
        writer.Write(mapId); //(MapID)
        writer.Write(mapId); //(SerialID)
        writer.Write(objId); //(CharacterId)
        writer.Write(x);
        writer.Write(y);
        writer.Write(z);
        writer.Write(rot);
        writer.Write((byte)0);
        return writer.ToBytes(); //33 bytes
    }
}
