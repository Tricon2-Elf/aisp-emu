namespace AISpace.Network.Data;

public class CircleData(uint id, string name, uint leaderId)
{
    public uint Id { get; set; } = id;
    public uint Status { get; set; } = 1;        // circle status flag
    public string Name { get; set; } = name;
    public uint LeaderId { get; set; } = leaderId;
    public string Date { get; set; } = "";
    public byte[] Unk_20 { get; set; } = new byte[20]; // unknown
    public string Message { get; set; } = "";

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(Id);
        writer.Write(Status);
        writer.WriteFixedString(Name, 46, "Shift_JIS");
        writer.Write(LeaderId);
        writer.WriteFixedString(Date, 37, "Shift_JIS");
        writer.Write(Unk_20);
        writer.WriteFixedString(Message, 751, "Shift_JIS");
        return writer.ToBytes();
    }
}
