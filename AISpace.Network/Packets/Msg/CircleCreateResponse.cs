using AISpace.Network;
using AISpace.Network.Data;

namespace AISpace.Network.Packets.Msg;

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
