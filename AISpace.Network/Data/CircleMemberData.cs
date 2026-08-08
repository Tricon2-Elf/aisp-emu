namespace AISpace.Network.Data;

public class CircleMemberData
{
    public uint AvatarId;
    public string Name = string.Empty;
    public uint Role;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();

        writer.Write(AvatarId);

        writer.WriteFixedString(Name, 37);
        writer.Write(Role);

        return writer.ToBytes();
    }
}
