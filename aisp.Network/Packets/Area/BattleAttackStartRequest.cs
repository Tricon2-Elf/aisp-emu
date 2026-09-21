using System.Numerics;
using aisp.Network;

namespace aisp.Network.Packets.Area;

/// <summary>
/// <c>send_battle_attack_start</c>: aim sample. Layout is <c>target_pos</c> then <c>now_pos</c> (two LE vec3s).
/// </summary>
public sealed class BattleAttackStartRequest(Vector3 targetPos, Vector3 nowPos)
    : IIncomingPacket<BattleAttackStartRequest>
{
    public Vector3 TargetPos { get; } = targetPos;
    public Vector3 NowPos { get; } = nowPos;

    public static BattleAttackStartRequest FromBytes(ReadOnlySpan<byte> data)
    {
        if (data.Length < 24)
            return new BattleAttackStartRequest(Vector3.Zero, Vector3.Zero);

        var reader = new PacketReader(data);
        return new BattleAttackStartRequest(
            new Vector3(reader.ReadFloat(), reader.ReadFloat(), reader.ReadFloat()),
            new Vector3(reader.ReadFloat(), reader.ReadFloat(), reader.ReadFloat())
        );
    }
}
