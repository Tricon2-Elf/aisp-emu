namespace aisp.Network.Packets.Area;

/// <summary>recv_quest_updated_restsec (0x433A): u32 questid, u32 restsec.</summary>
public sealed class QuestUpdatedRestsecNotify(uint questId, uint restSec) : IOutgoingPacket
{
    public uint QuestId { get; } = questId;
    public uint RestSec { get; } = restSec;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(QuestId);
        writer.Write(RestSec);
        return writer.ToBytes();
    }

    public static QuestUpdatedRestsecNotify FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        return new QuestUpdatedRestsecNotify(reader.ReadUInt(), reader.ReadUInt());
    }
}
