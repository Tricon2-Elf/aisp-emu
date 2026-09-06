namespace aisp.Network.Packets.Area;

/// <summary>recv_quest_remove_target (0xCB08): u32 questid.</summary>
public sealed class QuestRemoveTargetNotify(uint questId) : IOutgoingPacket
{
    public uint QuestId { get; } = questId;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(QuestId);
        return writer.ToBytes();
    }

    public static QuestRemoveTargetNotify FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        return new QuestRemoveTargetNotify(reader.ReadUInt());
    }
}
