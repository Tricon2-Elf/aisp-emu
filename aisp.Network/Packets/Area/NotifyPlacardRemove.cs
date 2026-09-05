namespace aisp.Network.Packets.Area;

public sealed class NotifyPlacardRemove(uint placardId) : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(placardId);
        return writer.ToBytes();
    }
}
