using AISpace.Network;

namespace AISpace.Network.Packets.Area;

public class UpdateOptionResponse : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write((uint)1); //Result
        return writer.ToBytes();
    }
}
