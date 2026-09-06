namespace aisp.Network.Packets.Area;

/// <summary>recv_quest_ended (0xB02C): u32 questid.</summary>
public sealed class QuestEndedNotify(uint questId) : IOutgoingPacket
{
    public uint QuestId { get; } = questId;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(QuestId);
        return writer.ToBytes();
    }

    public static QuestEndedNotify FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        return new QuestEndedNotify(reader.ReadUInt());
    }
}
