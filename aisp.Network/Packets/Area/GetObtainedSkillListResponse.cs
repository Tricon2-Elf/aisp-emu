using aisp.Network;

namespace aisp.Network.Packets.Area;

public class GetObtainedSkillListResponse(
    uint result,
    uint roboId,
    IReadOnlyList<uint>? skillIds = null
) : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var skills = skillIds ?? [];
        var writer = new PacketWriter();
        writer.Write(result);
        writer.Write(roboId);
        writer.Write((uint)skills.Count);
        foreach (var id in skills)
            writer.Write(id);
        return writer.ToBytes();
    }
}
