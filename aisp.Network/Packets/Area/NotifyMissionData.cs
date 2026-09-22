using aisp.Network;

namespace aisp.Network.Packets.Area;

public class NotifyMissionData(
    uint missionId = 603,
    string name = "ジョイント！！",
    uint timeLimitSeconds = 1800,
    string description = "制限時間内に40体をたおせ！！",
    uint targetCount = 40,
    MissionRuleType missionRuleType = MissionRuleType.FreeAim
) : IOutgoingPacket
{
    public const int WireSize = 463;

    /// <summary>
    /// <c>sub_4F09A0</c> stores <c>60 * minutes</c> at the mission object +76,
    /// which <c>IF::CTPSTimeLimitWindow</c> uses for MM:SS. Zero leaves <c>--:--</c>.
    /// </summary>
    public uint TimeLimitMinutes => timeLimitSeconds / 60;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();

        writer.Write(missionId);
        writer.WriteFixedString(name, 49, "Shift_JIS");
        writer.Write(0u);
        writer.Write((uint)missionRuleType);
        writer.Write(timeLimitSeconds);
        writer.WriteFixedString(description, 361, "Shift_JIS");
        writer.Write(0u);
        writer.Write(TimeLimitMinutes);
        writer.Write((byte)0);
        writer.Write(targetCount);

        for (var i = 0; i < 5; i++)
            writer.Write(0u);

        writer.Write(40990200u);

        return writer.ToBytes();
    }
}
