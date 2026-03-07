using AISpace.Network;

namespace AISpace.Network.Packets.Msg;

public class AvatarSelectResponse(uint result) : IPacket<AvatarSelectResponse>
{
    public static AvatarSelectResponse FromBytes(ReadOnlySpan<byte> data)
    {
        throw new NotImplementedException();
    }

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(result);
        return writer.ToBytes();
    }
}
