using System.Numerics;
using aisp.Network;

namespace aisp.Network.Packets.Area;

/// <summary>
/// <c>send_battle_attack_exec</c>: shot commit. Layout is a single LE vec3 <c>now_pos</c>.
/// </summary>
public sealed class BattleAttackExecRequest(Vector3 nowPos)
    : IIncomingPacket<BattleAttackExecRequest>
{
    public Vector3 NowPos { get; } = nowPos;

    public static BattleAttackExecRequest FromBytes(ReadOnlySpan<byte> data)
    {
        if (data.Length < 12)
            return new BattleAttackExecRequest(Vector3.Zero);

        var reader = new PacketReader(data);
        return new BattleAttackExecRequest(
            new Vector3(reader.ReadFloat(), reader.ReadFloat(), reader.ReadFloat())
        );
    }
}
