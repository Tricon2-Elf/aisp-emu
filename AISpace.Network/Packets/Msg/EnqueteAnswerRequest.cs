using AISpace.Network;

namespace AISpace.Network.Packets.Msg;

public class EnqueteAnswerRequest : IIncomingPacket<EnqueteAnswerRequest>
{
    List<uint> EnqueteIds = [];
    List<uint> AnswerIds = [];

    public static EnqueteAnswerRequest FromBytes(ReadOnlySpan<byte> data)
    {
        List<uint> QuestionIds = [];
        List<uint> answerIds = [];
        var reader = new PacketReader(data);

        for (int i = 0; i < reader.ReadUInt(); i++)
            QuestionIds.Add(reader.ReadUInt());
        for (int i = 0; i < reader.ReadUInt(); i++)
            answerIds.Add(reader.ReadUInt());
        var AnswerRequest = new EnqueteAnswerRequest { EnqueteIds = QuestionIds, AnswerIds = answerIds };

        return AnswerRequest;
    }
}
