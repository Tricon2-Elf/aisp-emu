namespace AISpace.Common.Network.Packets.Msg;

public class AvatarGetDataRequest : IPacket<AvatarGetDataRequest>
{
    public static AvatarGetDataRequest FromBytes(ReadOnlySpan<byte> data)
    {
        throw new NotImplementedException();
    }

    public byte[] ToBytes()
    {
        throw new NotImplementedException();
    }
}
