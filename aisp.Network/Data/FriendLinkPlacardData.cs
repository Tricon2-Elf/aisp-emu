using System.Numerics;

namespace aisp.Network.Data;

public sealed record FriendLinkPlacardData(
    uint PlacardId,
    string OwnerName,
    uint OwnerAvatarId,
    uint TagId,
    uint Slot,
    byte Direction,
    string TagName,
    Vector3 Position
)
{
    internal void Write(PacketWriter writer) =>
        Packets.Area.PlacardSettingResponse.WritePlacardData(
            writer,
            PlacardId,
            OwnerName,
            OwnerAvatarId,
            TagId,
            Slot,
            Direction,
            TagName,
            Position
        );
}
