namespace aisp.Network.Packets.Area;

public sealed class RequestAddFriendListRequest(uint targetAvatarId)
    : IIncomingPacket<RequestAddFriendListRequest>
{
    public uint TargetAvatarId { get; } = targetAvatarId;

    public static RequestAddFriendListRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        return new RequestAddFriendListRequest(reader.ReadUInt());
    }
}
