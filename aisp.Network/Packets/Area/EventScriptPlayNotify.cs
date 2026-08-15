using aisp.Network;

namespace aisp.Network.Packets.Area;

public sealed class EventScriptPlayNotify(string label) : IOutgoingPacket
{
    public string Label { get; } = label;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(Label, "utf-8");
        return writer.ToBytes();
    }
}
