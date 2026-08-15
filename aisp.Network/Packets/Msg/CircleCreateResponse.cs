using aisp.Network;
using aisp.Network.Data;

namespace aisp.Network.Packets.Msg;

public class CircleCreateResponse(uint result, CircleData? circle) : IOutgoingPacket
{
    public uint Result = result;
    public CircleData? Circle = circle;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(Result);
        (Circle ?? new CircleData()).Write(writer);
        return writer.ToBytes();
    }
}
