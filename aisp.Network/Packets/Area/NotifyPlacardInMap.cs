using System.Numerics;
using aisp.Network.Data;

namespace aisp.Network.Packets.Area;

public sealed class NotifyPlacardInMap(
    uint placardId,
    string ownerName,
    uint ownerAvatarId,
    uint tagId,
    uint slot,
    byte direction,
    string tagName = "",
    Vector3 position = default
) : IOutgoingPacket
{
    private readonly IReadOnlyList<FriendLinkPlacardData>? _placards;

    public NotifyPlacardInMap(IReadOnlyList<FriendLinkPlacardData> placards)
        : this(0, string.Empty, 0, 0, 0, 0)
    {
        _placards = placards;
    }

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        var placards =
            _placards
            ??
            [
                new FriendLinkPlacardData(
                    placardId,
                    ownerName,
                    ownerAvatarId,
                    tagId,
                    slot,
                    direction,
                    tagName,
                    position
                ),
            ];
        var count = Math.Min(placards.Count, 300);
        writer.Write((uint)count);
        for (var i = 0; i < count; i++)
            placards[i].Write(writer);
        return writer.ToBytes();
    }
}
