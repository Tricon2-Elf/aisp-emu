namespace AISpace.Common.Network.Packets.Area;

public class AdventureUploadRateGetResponse(uint Result = 1) : IPacket<AdventureUploadRateGetResponse>
{
    public static AdventureUploadRateGetResponse FromBytes(ReadOnlySpan<byte> data) => throw new NotImplementedException();

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(Result);
        return writer.ToBytes();
    }
}
