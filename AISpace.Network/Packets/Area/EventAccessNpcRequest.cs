using AISpace.Network;

namespace AISpace.Network.Packets.Area;

public class EventAccessNpcRequest : IIncomingPacket<EventAccessNpcRequest>
{
    public uint NpcId { get; set; }
    public float AvatarX { get; set; }
    public float AvatarY { get; set; }
    public float AvatarZ { get; set; }

    public static EventAccessNpcRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        return new EventAccessNpcRequest
        {
            NpcId = reader.ReadUInt(),
            AvatarX = reader.ReadFloat(),
            AvatarY = reader.ReadFloat(),
            AvatarZ = reader.ReadFloat(),
        };
    }
}
