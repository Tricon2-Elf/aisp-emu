using AISpace.Common.Network;

namespace AISpace.Common.Network.Packets.Msg;

public class CircleChatOutRequest : IPacket<CircleChatOutRequest>
{
    public static CircleChatOutRequest FromBytes(ReadOnlySpan<byte> data)
    {
        return new CircleChatOutRequest(); // Тело пустое или содержит ID, нужно проверить по логам/коду
    }

    public byte[] ToBytes() => Array.Empty<byte>();
}