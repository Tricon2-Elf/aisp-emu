namespace aisp.Network.Packets.Area;

public sealed class RoboAiscriptStartResponse(uint roboId, uint result) : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(roboId);
        writer.Write(result);
        return writer.ToBytes();
    }
}
