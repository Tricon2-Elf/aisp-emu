using AISpace.Network;

namespace AISpace.Network.Data;

public sealed class CircleData
{
    public const int WireSize = 866;
    public const int NameLength = 46;
    public const int MarkLength = 37;
    public const int DateLength = 20;
    public const int MessageLength = 751;
    public const int MaxCirclesPerCharacter = 15;

    public uint Id { get; set; }
    public uint Status { get; set; } = 1;
    public string Name { get; set; } = string.Empty;
    public uint LeaderId { get; set; }
    public string Mark { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;

    public CircleData() { }

    public CircleData(uint id, string name, uint leaderId)
    {
        Id = id;
        Name = name;
        LeaderId = leaderId;
    }

    public static CircleData Read(ref PacketReader reader) =>
        new()
        {
            Id = reader.ReadUInt(),
            Status = reader.ReadUInt(),
            Name = reader.ReadFixedString(NameLength, "utf-8"),
            LeaderId = reader.ReadUInt(),
            Mark = reader.ReadFixedString(MarkLength, "utf-8"),
            Date = reader.ReadFixedString(DateLength, "utf-8"),
            Message = reader.ReadFixedString(MessageLength, "utf-8"),
        };

    public void Write(PacketWriter writer)
    {
        writer.Write(Id);
        writer.Write(Status);
        writer.WriteFixedString(Name, NameLength, "utf-8");
        writer.Write(LeaderId);
        writer.WriteFixedString(Mark, MarkLength, "utf-8");
        writer.WriteFixedString(Date, DateLength, "utf-8");
        writer.WriteFixedString(Message, MessageLength, "utf-8");
    }

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        Write(writer);
        return writer.ToBytes();
    }
}
