namespace aisp.Network.Packets.Area;

public class SkillStartCastRequest(uint targetObjId, uint skillId, uint unknown, byte slotIndex)
    : IIncomingPacket<SkillStartCastRequest>
{
    public uint TargetObjId { get; } = targetObjId;
    public uint SkillId { get; } = skillId;
    public uint Unknown { get; } = unknown;
    public byte SlotIndex { get; } = slotIndex;

    public static SkillStartCastRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        return new SkillStartCastRequest(
            reader.ReadUInt(),
            reader.ReadUInt(),
            reader.ReadUInt(),
            reader.ReadByte()
        );
    }
}
