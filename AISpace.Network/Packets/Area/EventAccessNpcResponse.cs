using AISpace.Network;

namespace AISpace.Network.Packets.Area;

public class EventAccessNpcResponse(uint result) : IPacket<EventAccessNpcResponse>
{
    public static EventAccessNpcResponse FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        return new EventAccessNpcResponse(reader.ReadUInt());
    }

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(result);
        return writer.ToBytes();
    }
}
