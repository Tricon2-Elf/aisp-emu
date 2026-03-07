using AISpace.Network.Data;

namespace AISpace.Network.Packets.Area;

public class AvatarNotifyData(uint Result, AvatarData avatarData) : IPacket<AvatarNotifyData>
{
    public static AvatarNotifyData FromBytes(ReadOnlySpan<byte> data)
    {
        throw new NotImplementedException();
    }

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(Result);
        writer.Write(avatarData.ToBytes());
        return writer.ToBytes();
    }
}
