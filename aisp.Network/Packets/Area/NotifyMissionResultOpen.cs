using aisp.Network;

namespace aisp.Network.Packets.Area;

/// <summary>
/// recv_notify_mission_result_open: 252-byte result (<c>sub_79AA80</c>) plus
/// <c>others</c> count (max 4). The first loop of that reader is four
/// (value, extra) pairs. Clear time is client-side; the next three pairs are
/// Unable to Fight, Defeated Enemies, and Hit Rate (extras 0–4 → D–S).
/// The dword after those pairs (<c>a1[8]</c>, +32) is the overall rank
/// (<c>CMissionParts</c> <c>v6[4]</c> → resources 640–644). Putting a name at +8
/// made the first stat show SJIS/ASCII name bytes as an integer (e.g. "Tri" → 6910548).
/// </summary>
public sealed class NotifyMissionResultOpen(
    uint resultCode = 0,
    uint grade = 0,
    string playerName = "",
    uint unableToFight = 0,
    uint defeatedEnemies = 0,
    uint hitRatePercent = 0,
    uint clearTimeSeconds = 0
) : IOutgoingPacket
{
    public const int ResultWireSize = 252;
    public const int UnableToFightOffset = 8;
    public const int DefeatedOffset = 16;
    public const int HitRateOffset = 24;
    public const int OverallGradeOffset = 32;

    public byte[] ToBytes()
    {
        _ = playerName;
        _ = clearTimeSeconds;

        var writer = new PacketWriter();
        writer.Write(resultCode);
        writer.Write(grade);
        WritePair(writer, unableToFight, grade);
        WritePair(writer, defeatedEnemies, grade);
        WritePair(writer, hitRatePercent, grade);
        writer.Write(grade);
        writer.Write(new byte[ResultWireSize - (sizeof(uint) * 9)]);
        writer.Write(0u);
        return writer.ToBytes();
    }

    private static void WritePair(PacketWriter writer, uint value, uint extra)
    {
        writer.Write(value);
        writer.Write(extra);
    }
}
