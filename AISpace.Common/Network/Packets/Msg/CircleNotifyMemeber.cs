using AISpace.Common.Network;
using System.Text;

namespace AISpace.Common.Network.Packets.Msg;

public class CircleMemberData
{
    public uint AvatarId;
    public string Name = string.Empty;
    public uint Role; 

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        
        // 1. AvatarId (4 байта)
        writer.Write(AvatarId); 

        // 2. Name (37 байт)
        byte[] nameBytes = Encoding.GetEncoding("Shift_JIS").GetBytes(Name);
        byte[] finalName = new byte[37];
        Array.Copy(nameBytes, finalName, Math.Min(nameBytes.Length, 36));
        writer.Write(finalName);

        // 3. Role (4 байта)
        writer.Write(Role);

        return writer.ToBytes(); // 45 байт
    }
}

public class CircleNotifyMember(uint circleId, List<CircleMemberData> members) : IPacket<CircleNotifyMember>
{
    public static CircleNotifyMember FromBytes(ReadOnlySpan<byte> data) => throw new NotImplementedException();

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();

        // 1. CircleID (8 байт)
        writer.Write(circleId);
        writer.Write((uint)0);

        // 2. Количество участников (4 байта)
        writer.Write((uint)members.Count); 

        // 3. Данные участников (по 45 байт)
        foreach (var member in members)
        {
            writer.Write(member.ToBytes());
        }

        // 4. Второй счетчик для Already_Login (4 байта)
        writer.Write((uint)members.Count); 
        
        // 5. Статусы (по 1 байту)
        foreach (var member in members)
        {
            writer.Write((byte)1); 
        }

        return writer.ToBytes();
    }
}