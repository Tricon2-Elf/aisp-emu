namespace aisp.Network.Packets.Area;

public sealed class GetAiPaletteListResponse(uint result, uint roboId) : IOutgoingPacket
{
    private const int SlotCount = 36;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(result);
        writer.Write(roboId);
        for (var i = 0; i < SlotCount; i++)
            writer.Write(0u);
        for (var i = 0; i < SlotCount; i++)
            writer.Write(0u);
        return writer.ToBytes();
    }
}
