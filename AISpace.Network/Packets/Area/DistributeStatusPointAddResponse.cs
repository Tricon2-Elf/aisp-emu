namespace AISpace.Network.Packets.Area;

/// <summary>Result, Robo ID, status category, and the point cost for the requested value.</summary>
public sealed class DistributeStatusPointAddResponse(uint result, uint roboId, uint type, uint cost) : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(result);
        writer.Write(roboId);
        writer.Write(type);
        writer.Write(cost);
        return writer.ToBytes();
    }
}
