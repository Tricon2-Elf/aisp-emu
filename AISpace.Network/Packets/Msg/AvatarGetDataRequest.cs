using AISpace.Network;

namespace AISpace.Network.Packets.Msg;

public class AvatarGetDataRequest(uint result) : IPacket<AvatarGetDataRequest>
{
    public static AvatarGetDataRequest FromBytes(ReadOnlySpan<byte> data)
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
