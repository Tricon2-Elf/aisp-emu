using AISpace.Network;

namespace AISpace.Network.Packets.Area;

public class NotifySupplyNpcExec(uint objId) : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(objId);
        return writer.ToBytes();
    }
}
