namespace aisp.Network.Packets.Area;

/// <summary>
/// Tells the client controlling a Robo that an avatar ended its interaction.
/// Payload: UInt RoboId + UInt AvatarObjectId.
/// </summary>
public sealed class RoboDetachNoticeFromAvatarNotify(uint roboId, uint avatarObjectId)
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
