using System.Numerics;
using aisp.Network;

namespace aisp.Network.Packets.Area;

/// <summary>
/// <c>send_battle_attack_blaze</c>: stream aim sample while <c>CTPSTimeBlaze</c> is active.
/// Layout matches start: <c>target_pos</c> then <c>now_pos</c> (two LE vec3s).
/// </summary>
public sealed class BattleAttackBlazeRequest(Vector3 targetPos, Vector3 nowPos)
    : IIncomingPacket<BattleAttackBlazeRequest>
{
    public Vector3 TargetPos { get; } = targetPos;
    public Vector3 NowPos { get; } = nowPos;

    public static BattleAttackBlazeRequest FromBytes(ReadOnlySpan<byte> data)
    {
        if (data.Length < 24)
            return new BattleAttackBlazeRequest(Vector3.Zero, Vector3.Zero);

        var reader = new PacketReader(data);
        return new BattleAttackBlazeRequest(
            new Vector3(reader.ReadFloat(), reader.ReadFloat(), reader.ReadFloat()),
            new Vector3(reader.ReadFloat(), reader.ReadFloat(), reader.ReadFloat())
        );
    }
}
