namespace AISpace.Network.Data;

public enum CircleMemberRole : uint
{
    Core = 0,    // officer
    Normal = 1,  // regular member
}

public class CircleMemberData
{
    public uint AvatarId;
    public string Name = string.Empty;
    public CircleMemberRole Role;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(AvatarId);
        writer.WriteFixedString(Name, 37, "Shift_JIS");
        writer.Write((uint)Role);
        return writer.ToBytes();
    }
}
