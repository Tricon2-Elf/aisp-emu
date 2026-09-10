namespace aisp.Network.Packets.Area;

public class SkillStartCastResponse(uint result, float delay) : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(result);
        writer.Write(delay);
        return writer.ToBytes();
    }
}
