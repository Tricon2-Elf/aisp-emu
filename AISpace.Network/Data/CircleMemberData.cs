using AISpace.Network;

namespace AISpace.Network.Data;

public sealed class CircleMemberData
{
    // Wire: avatarId(4) + name[37] + role(4). Client in-memory stride is 48 with pad after name; that pad is not on the wire.
    public const int WireSize = 45;
    public const int NameLength = 37;
    public const int MaxMembers = 100;

    public const uint RoleMember = 0;
    public const uint RoleCore = 1;
    public const uint RoleLeader = 2;

    public uint AvatarId { get; set; }
    public string Name { get; set; } = string.Empty;
    public uint Role { get; set; }

    public static CircleMemberData Read(ref PacketReader reader) =>
        new()
        {
            AvatarId = reader.ReadUInt(),
            Name = reader.ReadFixedString(NameLength, "utf-8"),
            Role = reader.ReadUInt(),
        };

    public void Write(PacketWriter writer)
    {
        writer.Write(AvatarId);
        writer.WriteFixedString(Name, NameLength, "utf-8");
        writer.Write(Role);
    }

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        Write(writer);
        return writer.ToBytes();
    }
}
