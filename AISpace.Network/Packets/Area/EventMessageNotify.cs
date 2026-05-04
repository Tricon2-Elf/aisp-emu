namespace AISpace.Network.Packets.Area;

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
        writer.WriteFixedString(Name, 37, "Shift_JIS");
        writer.WriteFixedString(Text, 1537, "Shift_JIS");
        return writer.ToBytes();
    }
}
