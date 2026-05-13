using AISpace.Network;

namespace AISpace.Network.Packets.Area;

public class OtherProfileTextRequest(uint targetObjectId) : IIncomingPacket<OtherProfileTextRequest>
{
    public uint TargetObjectId { get; } = targetObjectId;

    public static OtherProfileTextRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        return new OtherProfileTextRequest(reader.ReadUInt());
    }
}
