using AISpace.Network;

namespace AISpace.Network.Packets.Msg;

public class CircleMarkChangeRequest : IIncomingPacket<CircleMarkChangeRequest>
{
    public ulong CircleId;
    public uint MarkId;

    public static CircleMarkChangeRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        return new CircleMarkChangeRequest
        {
            CircleId = reader.ReadULong(),
            MarkId = reader.ReadUInt(),
        };
    }
}
