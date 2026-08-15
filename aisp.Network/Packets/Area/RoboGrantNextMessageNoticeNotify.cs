namespace aisp.Network.Packets.Area;

/// <summary>
/// Allows the client-side Robo script to produce its next conversation message.
/// Payload: UInt RoboId.
/// </summary>
public sealed class RoboGrantNextMessageNoticeNotify(uint roboId) : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(roboId);
        return writer.ToBytes();
    }
}
