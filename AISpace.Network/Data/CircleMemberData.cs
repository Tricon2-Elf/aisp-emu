using AISpace.Network;

namespace AISpace.Network.Data;

public sealed class CircleMemberData
{
    public const int WireSize = 48;
    public const int NameLength = 37;
    public const int NamePadding = 3;
    public const int MaxMembers = 100;

    public const uint RoleMember = 0;
    public const uint RoleCore = 1;
    public const uint RoleLeader = 2;

    public uint AvatarId { get; set; }
    public string Name { get; set; } = string.Empty;
    public uint Role { get; set; }

    public static CircleMemberData Read(ref PacketReader reader)
    {
        var member = new CircleMemberData
        {
            AvatarId = reader.ReadUInt(),
            Name = reader.ReadFixedString(NameLength),
        };
        reader.ReadBytes(NamePadding);
        member.Role = reader.ReadUInt();
        return member;
    }

    public void Write(PacketWriter writer)
    {
        writer.Write(AvatarId);
        writer.WriteFixedString(Name, NameLength);
        writer.Write(new byte[NamePadding]);
        writer.Write(Role);
    }

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        Write(writer);
        return writer.ToBytes();
    }
}
