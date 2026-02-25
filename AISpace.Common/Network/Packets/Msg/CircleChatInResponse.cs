using AISpace.Common.Network;

namespace AISpace.Common.Network.Packets.Msg;

public class CircleChatInResponse(uint result) : IPacket<CircleChatInResponse>
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(result);
        return writer.ToBytes();
    }

    public static CircleChatInResponse FromBytes(ReadOnlySpan<byte> data) => throw new NotImplementedException();
}