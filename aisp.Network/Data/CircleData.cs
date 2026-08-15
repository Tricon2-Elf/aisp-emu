using aisp.Network;

namespace aisp.Network.Data;

public sealed class CircleData
{
    // Wire: u64 id + name[46] + markId(u32) + author[37] + date[20] + message[751] = 866.
    // markId is the icon; author is the last message-board editor name (also updated by notify).
    public const int WireSize = 866;
    public const int NameLength = 46;
    public const int AuthorLength = 37;
    public const int DateLength = 20;
    public const int MessageLength = 751;
    public const int MaxCirclesPerCharacter = 15;

    public ulong Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public uint MarkId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;

    public CircleData() { }

    public CircleData(ulong id, string name, uint markId)
    {
        Id = id;
        Name = name;
        MarkId = markId;
    }

    public static CircleData Read(ref PacketReader reader) =>
        new()
        {
            Id = reader.ReadULong(),
            Name = reader.ReadFixedString(NameLength, "utf-8"),
            MarkId = reader.ReadUInt(),
            AuthorName = reader.ReadFixedString(AuthorLength, "utf-8"),
            Date = reader.ReadFixedString(DateLength, "utf-8"),
            Message = reader.ReadFixedString(MessageLength, "utf-8"),
        };

    public void Write(PacketWriter writer)
    {
        writer.Write(Id);
        writer.WriteFixedString(Name, NameLength, "utf-8");
        writer.Write(MarkId);
        writer.WriteFixedString(AuthorName, AuthorLength, "utf-8");
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
