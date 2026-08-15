namespace aisp.Network.Packets.Area;

/// <summary>
/// Displays a message produced by a Robo conversation.
/// Payload: UInt RoboId + null-terminated UTF-8 Message.
/// </summary>
public sealed class RoboTalkForwardNotify(uint roboId, string message) : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(roboId);
        writer.Write(message, "utf-8");
        return writer.ToBytes();
    }
}
