namespace aisp.Network.Packets.Area;

public class EventMessageNotify : IOutgoingPacket
{
    public uint ObjId { get; set; }
    public string Name { get; set; }
    public string Text { get; set; }

    public EventMessageNotify(uint objId, string name, string text)
    {
        ObjId = objId;
        Name = name;
        Text = text;
    }

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(ObjId);
        writer.Write(Name, "utf-8");
        writer.Write(Text, "utf-8");
        return writer.ToBytes();
    }
}
