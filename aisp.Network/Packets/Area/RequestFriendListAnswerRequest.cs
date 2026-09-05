namespace aisp.Network.Packets.Area;

public sealed class RequestFriendListAnswerRequest(uint answer)
    : IIncomingPacket<RequestFriendListAnswerRequest>
{
    public uint Answer { get; } = answer;

    public static RequestFriendListAnswerRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        return new RequestFriendListAnswerRequest(reader.ReadUInt());
    }
}
