using AISpace.Network;

namespace AISpace.Network.Packets.Area;

public sealed class ShopEndResponse(uint result) : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(result);
        return writer.ToBytes();
    }
}
