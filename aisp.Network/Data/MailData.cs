namespace aisp.Network.Data;

/// <summary>
/// Packed mail record from <c>ReadMailData</c> (client in-memory size 0x3C8 with padding; wire is 960 bytes).
/// </summary>
public sealed class MailData
{
    public const int WireSize = 960;
    public const int NameLength = 37;
    public const int DateLength = 20;
    public const int SubjectLength = 91;
    public const int BodyLength = 751;

    public ulong MailId { get; set; }
    public uint Type { get; set; }

    /// <summary>Unknown flags / status dword at wire offset 12 (protected/read, etc.).</summary>
    public uint Flags { get; set; }

    /// <summary>Sender character id at wire offset 16.</summary>
    public uint SenderId { get; set; }
    public string SenderName { get; set; } = string.Empty;

    /// <summary>Recipient character id at wire offset 0x3C.</summary>
    public uint DistId { get; set; }
    public string DistName { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;

    public static MailData Read(ref PacketReader reader) =>
        new()
        {
            MailId = reader.ReadULong(),
            Type = reader.ReadUInt(),
            Flags = reader.ReadUInt(),
            SenderId = reader.ReadUInt(),
            SenderName = reader.ReadFixedString(NameLength),
            DistId = reader.ReadUInt(),
            DistName = reader.ReadFixedString(NameLength),
            Date = reader.ReadFixedString(DateLength),
            Subject = reader.ReadFixedString(SubjectLength),
            Body = reader.ReadFixedString(BodyLength),
        };

    public static MailData FromBytes(ReadOnlySpan<byte> data)
    {
        if (data.Length < WireSize)
            throw new ArgumentException(
                $"MailData requires at least {WireSize} bytes.",
                nameof(data)
            );

        var reader = new PacketReader(data);
        return Read(ref reader);
    }

    public void Write(PacketWriter writer)
    {
        writer.Write(MailId);
        writer.Write(Type);
        writer.Write(Flags);
        writer.Write(SenderId);
        writer.WriteFixedString(SenderName, NameLength);
        writer.Write(DistId);
        writer.WriteFixedString(DistName, NameLength);
        writer.WriteFixedString(Date, DateLength);
        writer.WriteFixedString(Subject, SubjectLength);
        writer.WriteFixedString(Body, BodyLength);
    }

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        Write(writer);
        return writer.ToBytes();
    }
}
