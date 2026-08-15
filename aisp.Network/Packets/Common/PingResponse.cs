using aisp.Network;

namespace aisp.Network.Packets.Common;

public sealed class PingResponse : IOutgoingPacket
{
    public uint Time { get; }

    public PingResponse(uint time) => Time = time;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(Time);
        return writer.ToBytes();
    }
}
