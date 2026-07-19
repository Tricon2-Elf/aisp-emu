using AISpace.Network;
using AISpace.Network.Data;

namespace AISpace.Network.Packets.Area;

public class RoboGetListResponse(IReadOnlyList<RoboData>? robos = null) : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var list = robos ?? [];
        var writer = new PacketWriter();
        writer.Write(0u); // Result
        writer.Write((uint)list.Count);
        foreach (var robo in list)
            writer.Write(robo.ToBytes());
        return writer.ToBytes();
    }
}
