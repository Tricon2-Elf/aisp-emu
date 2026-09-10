namespace aisp.Network.Packets.Area;

public class SkillExecRequest(uint targetObjId, uint skillId, uint unknown, byte slotIndex)
    : IIncomingPacket<SkillExecRequest>
{
    public uint TargetObjId { get; } = targetObjId;
    public uint SkillId { get; } = skillId;
    public uint Unknown { get; } = unknown;
    public byte SlotIndex { get; } = slotIndex;

    public static SkillExecRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        return new SkillExecRequest(
            reader.ReadUInt(),
            reader.ReadUInt(),
            reader.ReadUInt(),
            reader.ReadByte()
        );
    }
}
