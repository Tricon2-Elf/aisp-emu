namespace aisp.Network.Packets.Area;

/// <summary>recv_quest_update_target (0x710C): u32 questid, u16 now.</summary>
public sealed class QuestUpdateTargetNotify(uint questId, ushort now) : IOutgoingPacket
{
    public uint QuestId { get; } = questId;
    public ushort Now { get; } = now;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(QuestId);
        writer.Write(Now);
        return writer.ToBytes();
    }

    public static QuestUpdateTargetNotify FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        return new QuestUpdateTargetNotify(reader.ReadUInt(), reader.ReadUShort());
    }
}
