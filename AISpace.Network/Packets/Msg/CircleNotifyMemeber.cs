using System.Text;
using AISpace.Network;

namespace AISpace.Network.Packets.Msg;

public class CircleMemberData
{
    public uint AvatarId;
    public string Name = string.Empty;
    public uint Role;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();

        // 1. AvatarId
        writer.Write(AvatarId);

        // 2. Name
        byte[] nameBytes = Encoding.GetEncoding("Shift_JIS").GetBytes(Name);
        byte[] finalName = new byte[37];
        Array.Copy(nameBytes, finalName, Math.Min(nameBytes.Length, 36));
        writer.Write(finalName);

        // 3. Role
        writer.Write(Role);

        return writer.ToBytes();
    }
}

public class CircleNotifyMember(uint circleId, List<CircleMemberData> members) : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();

        // 1. CircleID
        writer.Write(circleId);
        writer.Write((uint)0);

        // 2. Number of members (4 bytes)
        writer.Write((uint)members.Count);

        // 3. Member data (45 bytes each)
        foreach (var member in members)
        {
            writer.Write(member.ToBytes());
        }

        // 4. Second count for Already_Login (4 bytes)
        writer.Write((uint)members.Count);

        // 5. Statuses (1 byte each)
        foreach (var member in members)
        {
            writer.Write((byte)1);
        }

        return writer.ToBytes();
    }
}
