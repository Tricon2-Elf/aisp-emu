using aisp.Network;
using aisp.Network.Data;

namespace aisp.Network.Packets.Area;

public class FriendGetListDataResponse(
    IReadOnlyList<FriendData>? friends = null,
    IReadOnlyList<bool>? online = null
) : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write((uint)0);

        var friendList = friends ?? [];
        var friendCount = Math.Min(friendList.Count, FriendData.MaxFriends);
        writer.Write((uint)friendCount);
        for (var i = 0; i < friendCount; i++)
            friendList[i].Write(writer);

        var onlineList = online ?? [];
        var onlineCount = Math.Min(onlineList.Count, friendCount);
        writer.Write((uint)onlineCount);
        for (var i = 0; i < onlineCount; i++)
            writer.Write((byte)(onlineList[i] ? 1 : 0));

        // Per-friend comments are not persisted yet.
        writer.Write((uint)0);
        return writer.ToBytes();
    }
}
