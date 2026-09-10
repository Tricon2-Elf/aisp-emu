using aisp.Network;

namespace aisp.Network.Packets.Area;

public class NotifyMissionData(
    uint missionId = 603,
    string name = "ジョイント！！",
    uint timeLimitSeconds = 300,
    string description = "制限時間内に40体をたおせ！！",
    uint targetCount = 40
) : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();

        // Exactly 463 bytes
        writer.Write(missionId); // 4 bytes (603)
        writer.WriteFixedString(name, 49, "Shift_JIS"); // 49 bytes (name)
        writer.Write(0u); // 4 bytes
        writer.Write(0u); // 4 bytes
        writer.Write(timeLimitSeconds); // 4 bytes (timer)
        writer.WriteFixedString(description, 361, "Shift_JIS"); // 361 bytes (objective description)
        writer.Write(0u); // 4 bytes
        writer.Write(0u); // 4 bytes
        writer.Write((byte)0); // 1 byte (Status)
        writer.Write(targetCount); // 4 bytes (objective: 40 mobs)

        for (var i = 0; i < 5; i++)
            writer.Write(0u); // 20 bytes (rewards)

        writer.Write(40990200u); // 4 bytes (MapId)

        return writer.ToBytes();
    }
}
