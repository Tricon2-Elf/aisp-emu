
namespace AISpace.Common.Network.Packets.Msg;

public class EnqueteAnswerRequest : IPacket<EnqueteAnswerRequest>
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
        var AnswerRequest = new EnqueteAnswerRequest
        {
            EnqueteIds = QuestionIds,
            AnswerIds = answerIds
        };

        return AnswerRequest;

    }

    public byte[] ToBytes()
    {
        throw new NotImplementedException();
    }
}
