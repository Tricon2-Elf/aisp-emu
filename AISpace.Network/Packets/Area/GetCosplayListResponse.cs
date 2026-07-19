namespace AISpace.Network.Packets.Area;

public sealed class GetCosplayListResponse(uint result, uint roboId) : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(result);
        writer.Write(roboId);
        writer.Write(0u); // count
        return writer.ToBytes();
    }
}
