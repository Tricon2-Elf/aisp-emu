namespace AISpace.Common.Network.Packets.Area;

public class GetMyAvatarMyprofileDataResponse(uint result) : IPacket<GetMyAvatarMyprofileDataResponse>
{
    public const int ProfileSize = 1280;

    public uint Result = result;

    public static GetMyAvatarMyprofileDataResponse FromBytes(ReadOnlySpan<byte> data)
    {
        throw new NotImplementedException();
    }

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(Result);
        writer.WriteFixedString("Sup yall", ProfileSize, "ASCII");
        return writer.ToBytes();
    }
}
