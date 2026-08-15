using aisp.Network;

namespace aisp.Network.Packets.Area;

public sealed class EventAreaMapSelectCloseNotify(uint result = 0) : IOutgoingPacket
{
    public uint Result { get; } = result;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(Result);
        return writer.ToBytes();
    }
}
