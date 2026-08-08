using AISpace.Network;

namespace AISpace.Network.Packets.Msg;

public class CircleMemberJoinAnswerRequest : IIncomingPacket<CircleMemberJoinAnswerRequest>
{
    public uint Answer;

    public static CircleMemberJoinAnswerRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        return new CircleMemberJoinAnswerRequest { Answer = reader.ReadUInt() };
    }
}
