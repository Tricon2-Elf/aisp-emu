using AISpace.Network;

namespace AISpace.Network.Packets.Area;

public class MissionDataResponse : IPacket<MissionDataResponse>
{
    public static MissionDataResponse FromBytes(ReadOnlySpan<byte> data)
    {
        throw new NotImplementedException();
    }

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write((uint)0); //Result
        return writer.ToBytes();
    }
}
