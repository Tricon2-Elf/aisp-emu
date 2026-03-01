namespace AISpace.Common.Network.Packets.Area;

public class EventAccessNpcRequest : IPacket<EventAccessNpcRequest>
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
            AvatarZ = reader.ReadFloat()
        };
    }

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(NpcId);
        writer.Write(AvatarX);
        writer.Write(AvatarY);
        writer.Write(AvatarZ);
        return writer.ToBytes();
    }
}
