using System.Text;
using AISpace.Network;

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

        byte[] nameBytes = Encoding.GetEncoding("Shift_JIS").GetBytes(Name);
        byte[] finalName = new byte[37];
        Array.Copy(nameBytes, finalName, Math.Min(nameBytes.Length, 36));
        writer.Write(finalName);

        writer.Write(Role);

        return writer.ToBytes();
    }
}
