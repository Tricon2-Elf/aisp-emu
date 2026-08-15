using aisp.Network;

namespace aisp.Network.Packets.Area;

public sealed class EventScriptPlayRequest(uint result) : IIncomingPacket<EventScriptPlayRequest>
{
    public uint Result { get; } = result;

    public static EventScriptPlayRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        return new EventScriptPlayRequest(reader.ReadUInt());
    }
}
