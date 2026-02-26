/* using AISpace.Common.Network;

namespace AISpace.Common.Network.Packets.Area;

public class NotifyMyRoomFurnitureList : IPacket<NotifyMyRoomFurnitureList>
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write((uint)0);
        writer.Write((uint)0);
        return writer.ToBytes();
    }

    public static NotifyMyRoomFurnitureList FromBytes(ReadOnlySpan<byte> data) => throw new NotImplementedException();
} */