namespace AISpace.Network.Packets.Area;

/// <summary>
/// Clears the avatar-side relationship after a Robo-side interaction detach.
/// Payload: UInt RoboId + UInt AvatarObjectId.
/// </summary>
public sealed class RoboDetachNoticeFromRoboNotify(uint roboId, uint avatarObjectId)
    : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(roboId);
        writer.Write(avatarObjectId);
        return writer.ToBytes();
    }
}
