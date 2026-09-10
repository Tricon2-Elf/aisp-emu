using aisp.Network;

namespace aisp.Network.Packets.Area;

public class NotifyMissionStartData(
    uint partyId = 1,
    uint missionId = 603,
    string missionName = "ジョイント！！",
    uint timeLimitSeconds = 300,
    string description = "制限時間内に40体をたおせ！！",
    uint targetCount = 40,
    uint leaderCharacterId = 0,
    uint characterId = 0,
    string characterName = "Player"
) : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();

        writer.Write(partyId);
        writer.Write(missionId);
        writer.WriteFixedString(missionName, 37, "Shift_JIS");
        // FIX: write the real LeaderCharacterId
        writer.Write(leaderCharacterId);
        writer.Write(0u);
        writer.Write(timeLimitSeconds);
        writer.WriteFixedString(description, 193, "Shift_JIS");
        writer.Write((byte)0);
        writer.Write(targetCount);
        writer.Write(0u);

        writer.Write(1u); // Member count

        // member_t
        writer.Write(characterId);
        writer.Write(characterId);
        writer.WriteFixedString(characterName, 37, "Shift_JIS");
        writer.Write((byte)1); // 1 = Leader Role
        writer.Write(0u);
        writer.WriteFixedString("", 37, "Shift_JIS");
        writer.Write(0u);

        writer.Write((byte)0); // is_force_cosplay

        return writer.ToBytes();
    }
}
