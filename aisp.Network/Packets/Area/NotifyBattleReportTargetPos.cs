using System.Numerics;
using aisp.Network;

namespace aisp.Network.Packets.Area;

/// <summary>
/// recv_notify_battle_report_target_pos: 13-byte report + target vec3 + effect list.
/// Used for free-aim shots (decomp <c>sub_798E10</c> + <c>ReadVec3</c>).
/// </summary>
public sealed class NotifyBattleReportTargetPos(
    uint attackerObjectId,
    uint actionType,
    byte actionFlags,
    uint skillId,
    Vector3 targetPos
) : IOutgoingPacket
{
    public uint AttackerObjectId { get; } = attackerObjectId;
    public uint ActionType { get; } = actionType;
    public byte ActionFlags { get; } = actionFlags;
    public uint SkillId { get; } = skillId;
    public Vector3 TargetPos { get; } = targetPos;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(AttackerObjectId);
        writer.Write(ActionType);
        writer.Write(ActionFlags);
        writer.Write(SkillId);
        writer.Write(TargetPos.X);
        writer.Write(TargetPos.Y);
        writer.Write(TargetPos.Z);
        writer.Write(0u);
        return writer.ToBytes();
    }
}
