namespace aisp.Network.Packets.Area;

public sealed class NotifyRequestFriendList(uint fromAvatarId, string fromName) : IOutgoingPacket
{
    public const int NameLength = 37;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(fromAvatarId);
        writer.WriteFixedString(fromName, NameLength);
        return writer.ToBytes();
    }
}
