namespace aisp.Network.Packets.Area;

public sealed class NotifyRequestFriendList(uint fromAvatarId, string fromName) : IOutgoingPacket
{
    public const int MaxNameBytes = 36;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(fromAvatarId);
        writer.Write(fromName, MaxNameBytes);
        return writer.ToBytes();
    }
}
