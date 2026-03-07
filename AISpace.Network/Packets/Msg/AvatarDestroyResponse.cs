using AISpace.Network;

namespace AISpace.Network.Packets.Msg;

public class AvatarDestroyResponse(uint result) : IPacket<AvatarDestroyResponse>
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(result); // 0
        return writer.ToBytes();
    }

    public static AvatarDestroyResponse FromBytes(ReadOnlySpan<byte> data) => throw new NotImplementedException();
}
