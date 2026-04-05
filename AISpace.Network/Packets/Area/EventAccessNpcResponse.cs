using AISpace.Network;

namespace AISpace.Network.Packets.Area;

public class EventAccessNpcResponse : IOutgoingPacket
{
    public uint Result { get; }

    public EventAccessNpcResponse(uint result) => Result = result;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(Result);
        return writer.ToBytes();
    }
}
