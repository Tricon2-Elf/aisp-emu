using aisp.Network;

namespace aisp.Network.Packets.Area;

public class GetObtainedSkillListRequest(uint roboId) : IIncomingPacket<GetObtainedSkillListRequest>
{
    public uint RoboId { get; } = roboId;

    public static GetObtainedSkillListRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        return new GetObtainedSkillListRequest(reader.ReadUInt());
    }
}
