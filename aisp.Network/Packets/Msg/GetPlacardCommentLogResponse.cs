namespace aisp.Network.Packets.Msg;

/// <summary>Acknowledges a placard-comment history request. Entries arrive through notifications.</summary>
public sealed class GetPlacardCommentLogResponse(uint result) : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(result);
        return writer.ToBytes();
    }
}
