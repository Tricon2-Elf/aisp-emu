using AISpace.Network;
using AISpace.Network.Data;

namespace AISpace.Network.Packets.Msg;

public class CircleNotifyMember(
    ulong circleId,
    IReadOnlyList<CircleMemberData> members,
    IReadOnlyList<bool> alreadyLogin
) : IOutgoingPacket
{
    public CircleNotifyMember(ulong circleId, IReadOnlyList<CircleMemberData> members)
        : this(circleId, members, [.. members.Select(_ => false)]) { }

    // Compatibility ctor used by older call sites.
    public CircleNotifyMember(uint circleId, List<CircleMemberData> members)
        : this((ulong)circleId, members, [.. members.Select(_ => true)]) { }

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(circleId);
        var count = Math.Min(members.Count, CircleMemberData.MaxMembers);
        writer.Write((uint)count);
        for (var i = 0; i < count; i++)
            members[i].Write(writer);
        var loginCount = Math.Min(alreadyLogin.Count, CircleMemberData.MaxMembers);
        writer.Write((uint)loginCount);
        for (var i = 0; i < loginCount; i++)
            writer.Write((byte)(alreadyLogin[i] ? 1 : 0));
        return writer.ToBytes();
    }
}
