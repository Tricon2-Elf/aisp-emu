using AISpace.Network;

namespace AISpace.Network.Packets.Area;

public class AvatarProfileGetDataRequest(uint targetObjectId)
    : IIncomingPacket<AvatarProfileGetDataRequest>
{
    public uint TargetObjectId { get; } = targetObjectId;

    public static AvatarProfileGetDataRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        return new AvatarProfileGetDataRequest(reader.ReadUInt());
    }
}
