using aisp.Network;
using aisp.Network.Data;

namespace aisp.Network.Packets.Msg;

public class CircleGetDataResponse(
    uint result,
    IReadOnlyList<(CircleData Circle, uint AuthLevel)> memberships
) : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(result);
        var count = Math.Min(memberships.Count, CircleData.MaxCirclesPerCharacter);
        writer.Write((uint)count);
        for (var i = 0; i < count; i++)
            memberships[i].Circle.Write(writer);
        writer.Write((uint)count);
        for (var i = 0; i < count; i++)
            writer.Write(memberships[i].AuthLevel);
        return writer.ToBytes();
    }
}
