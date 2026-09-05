namespace aisp.Network.Packets.Area;

public sealed class NotifyDeleteFriendListAvatar(uint avatarId) : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(avatarId);
        return writer.ToBytes();
    }
}
