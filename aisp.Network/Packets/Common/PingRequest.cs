using aisp.Network;

namespace aisp.Network.Packets.Common;

public sealed class PingRequest(uint time) : IIncomingPacket<PingRequest>
{
    public uint Time { get; } = time;

    public static PingRequest FromBytes(ReadOnlySpan<byte> data)
    {
        PacketReader reader = new(data);
        return new PingRequest(reader.ReadUInt());
    }
}
