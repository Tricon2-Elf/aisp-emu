namespace aisp.Network.Packets.Area;

public sealed class FriendResultResponse(uint result) : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(result);
        return writer.ToBytes();
    }
}

public sealed class NotifyRequestFriendList(uint fromAvatarId, string fromName) : IOutgoingPacket
{
    public const int NameLength = 37;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(fromAvatarId);
        writer.Write(fromName, NameLength - 1);
        return writer.ToBytes();
    }
}

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
