namespace AISpace.Common.Network.Packets.Msg;

public class AvatarGetDataResponse(uint result) : IPacket<AvatarGetDataResponse>
{
    public static AvatarGetDataResponse FromBytes(ReadOnlySpan<byte> data)
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
