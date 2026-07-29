namespace AISpace.Network.Packets.Area;

public sealed class NotifyMyRoomSetFurniture(MyRoomFurnitureData furniture) : IOutgoingPacket
{
    public MyRoomFurnitureData Furniture { get; } = furniture;

    public byte[] ToBytes() => Furniture.ToBytes();
}
