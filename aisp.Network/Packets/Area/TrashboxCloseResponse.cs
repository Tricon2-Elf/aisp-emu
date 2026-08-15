using aisp.Network;

namespace aisp.Network.Packets.Area;

public class TrashboxCloseResponse(uint result) : IOutgoingPacket
{
    public uint Result = result;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(Result);
        return writer.ToBytes();
    }
}
