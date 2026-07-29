using AISpace.Network.Data;

namespace AISpace.Network.Packets.Area;

public sealed class MyRoomNotifyFurniture(MyRoomFurnitureData furniture) : IOutgoingPacket
{
    public MyRoomFurnitureData Furniture { get; } = furniture;

    public byte[] ToBytes() => Furniture.ToBytes();
}
