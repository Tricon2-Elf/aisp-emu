using AISpace.Network;

namespace AISpace.Network.Packets.Msg;

public class CircleChatPostRequest : IIncomingPacket<CircleChatPostRequest>
{
    public uint MessageId;
    public string Message = string.Empty;

    public static CircleChatPostRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        return new CircleChatPostRequest
        {
            MessageId = reader.ReadUInt(),
            Message = reader.ReadString("utf-8"),
        };
    }
}
