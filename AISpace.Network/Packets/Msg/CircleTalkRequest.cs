using AISpace.Network;

namespace AISpace.Network.Packets.Msg;

public class CircleTalkRequest(uint messageId) : IIncomingPacket<CircleTalkRequest>
{
    public uint MessageId { get; } = messageId;

    public static CircleTalkRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        return new CircleTalkRequest(reader.ReadUInt());
    }
}
