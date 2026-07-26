namespace AISpace.Network.Packets.Area;

/// <summary>Result and Robo ID for the committed distributed status-point values.</summary>
public sealed class DistributeStatusPointFinishResponse(uint result, uint roboId) : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(result);
        writer.Write(roboId);
        return writer.ToBytes();
    }
}
