namespace aisp.Network.Packets.Area;

public sealed class DeleteFriendListRequest(uint avatarId)
    : IIncomingPacket<DeleteFriendListRequest>
{
    public uint AvatarId { get; } = avatarId;

    public static DeleteFriendListRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        return new DeleteFriendListRequest(reader.ReadUInt());
    }
}
