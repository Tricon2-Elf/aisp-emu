using AISpace.Network;

namespace AISpace.Network.Packets.Area;

public class MissionDataRequest : IPacket<MissionDataRequest>
{
    public static MissionDataRequest FromBytes(ReadOnlySpan<byte> data)
    {
        throw new NotImplementedException();
    }

    public byte[] ToBytes()
    {
        throw new NotImplementedException();
    }
}
