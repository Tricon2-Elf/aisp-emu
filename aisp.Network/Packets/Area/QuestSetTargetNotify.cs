namespace aisp.Network.Packets.Area;

/// <summary>recv_quest_set_target (0xE279): u32 questid, u16 tgt_required, null-terminated tgt_name (max 37).</summary>
public sealed class QuestSetTargetNotify(uint questId, ushort required, string targetName)
    : IOutgoingPacket
{
    public const int TargetNameMaxBytes = 37;

    public uint QuestId { get; } = questId;
    public ushort Required { get; } = required;
    public string TargetName { get; } = targetName;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(QuestId);
        writer.Write(Required);
        writer.Write(TargetName, TargetNameMaxBytes);
        return writer.ToBytes();
    }

    public static QuestSetTargetNotify FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        return new QuestSetTargetNotify(
            reader.ReadUInt(),
            reader.ReadUShort(),
            reader.ReadString()
        );
    }
}
