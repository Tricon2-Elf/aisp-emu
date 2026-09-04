namespace aisp.Network.Packets.Area;

/// <summary>Placard removal result.</summary>
public sealed class PlacardRemoveResponse(uint result) : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(result);
        return writer.ToBytes();
    }
}
