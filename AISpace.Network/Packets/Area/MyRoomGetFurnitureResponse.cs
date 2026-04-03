using AISpace.Network;

namespace AISpace.Network.Packets.Area;

public class MyRoomGetFurnitureResponse : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write((uint)0); //Result
        return writer.ToBytes();
    }
}
