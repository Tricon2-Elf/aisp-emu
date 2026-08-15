using aisp.Network;

namespace aisp.Network.Packets.Area;

public class FriendLinkTagGetRequest(uint targetObjectId) : IIncomingPacket<FriendLinkTagGetRequest>
{
    public uint TargetObjectId { get; } = targetObjectId;

    public static FriendLinkTagGetRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        return new FriendLinkTagGetRequest(reader.ReadUInt());
    }
}
