using AISpace.Network;

namespace AISpace.Network.Packets.Area;

public class MyRoomNotifyFurniture : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();

        writer.Write((uint)0);
        writer.Write((uint)1);
        writer.Write((uint)1);
        writer.Write((uint)11001550);
        writer.Write(0f);
        writer.Write(0f);
        writer.Write(0f);
        writer.Write(0f);
        writer.Write(0f);
        writer.Write(0f);
        writer.Write(1f);
        writer.Write((uint)0);
        return writer.ToBytes();
    }
}
