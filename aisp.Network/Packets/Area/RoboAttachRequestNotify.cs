namespace aisp.Network.Packets.Area;

/// <summary>
/// Asks the client controlling a Robo to begin an interaction with an avatar object.
/// Payload: UInt RoboId + UInt AvatarObjectId.
/// </summary>
public sealed class RoboAttachRequestNotify(uint roboId, uint avatarObjectId) : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(roboId);
        writer.Write(avatarObjectId);
        return writer.ToBytes();
    }
}
