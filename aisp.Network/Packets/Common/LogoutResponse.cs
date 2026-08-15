using aisp.Network;

namespace aisp.Network.Packets.Common;

public class LogoutResponse : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(0);
        return writer.ToBytes();
    }
}
