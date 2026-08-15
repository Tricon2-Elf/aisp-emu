namespace aisp.Network.Packets.Area;

/// <summary>
/// Server-to-client notice window (recv_event_notice / 0xCD6F).
/// Layout: name (nt, max 37), text (nt, max 1537), talkType (u32).
/// </summary>
public sealed class EventNoticeNotify : IOutgoingPacket
{
    public string Name { get; set; }
    public string Text { get; set; }
    public uint TalkType { get; set; }

    public EventNoticeNotify(string name, string text, uint talkType = 0)
    {
        Name = name;
        Text = text;
        TalkType = talkType;
    }

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(Name, "utf-8");
        writer.Write(Text, "utf-8");
        writer.Write(TalkType);
        return writer.ToBytes();
    }
}
