namespace AISpace.Network.Packets.Area;

/// <summary>
/// Completes the client protocol's Robo "attach" handshake used to enter Robo conversation.
/// Payload: UInt RoboId + UInt Result.
/// </summary>
public sealed class RoboAttachResponse(uint roboId, uint result) : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(roboId);
        writer.Write(result);
        return writer.ToBytes();
    }
}
