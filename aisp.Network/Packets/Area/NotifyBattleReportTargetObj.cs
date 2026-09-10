using aisp.Network;

namespace aisp.Network.Packets.Area;

/// <summary>
/// recv_notify_battle_report_target_obj: 13-byte report + target object id + effect list.
/// Report layout from sub_798E10: uint, uint, byte, uint.
/// </summary>
public sealed class NotifyBattleReportTargetObj(
    uint attackerObjectId,
    uint actionType,
    byte actionFlags,
    uint skillId,
    uint targetObjectId
) : IOutgoingPacket
{
    public const int ReportWireSize = 13;

    public uint AttackerObjectId { get; } = attackerObjectId;
    public uint ActionType { get; } = actionType;
    public byte ActionFlags { get; } = actionFlags;
    public uint SkillId { get; } = skillId;
    public uint TargetObjectId { get; } = targetObjectId;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(AttackerObjectId);
        writer.Write(ActionType);
        writer.Write(ActionFlags);
        writer.Write(SkillId);
        writer.Write(TargetObjectId);
        writer.Write(0u);
        return writer.ToBytes();
    }
}
