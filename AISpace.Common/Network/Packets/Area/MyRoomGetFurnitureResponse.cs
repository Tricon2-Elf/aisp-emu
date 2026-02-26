using AISpace.Common.Network;

namespace AISpace.Common.Network.Packets.Area;

public class MyRoomGetFurnitureResponse : IPacket<MyRoomGetFurnitureResponse>
{
    public static MyRoomGetFurnitureResponse FromBytes(ReadOnlySpan<byte> data) => throw new NotImplementedException();

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write((uint)0); 
        return writer.ToBytes();
    }
}
