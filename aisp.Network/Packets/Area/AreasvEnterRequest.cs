using aisp.Network;

namespace aisp.Network.Packets.Area;

public class AreasvEnterRequest : IIncomingPacket<AreasvEnterRequest>
{
    public required uint UserID;
    public required string OTP;

    public static AreasvEnterRequest FromBytes(ReadOnlySpan<byte> data)
    {
        PacketReader reader = new(data);
        AreasvEnterRequest req = new()
        {
            UserID = reader.ReadUInt(),
            OTP = reader.ReadFixedString(20, "ASCII"),
        };
        return req;
    }
}
