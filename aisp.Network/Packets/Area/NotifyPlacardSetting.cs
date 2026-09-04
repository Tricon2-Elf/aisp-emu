using System.Numerics;

namespace aisp.Network.Packets.Area;

public sealed class NotifyPlacardSetting(
    uint placardId,
    string ownerName,
    uint ownerAvatarId,
    uint tagId,
    uint slot,
    byte direction,
    string tagName,
    Vector3 position
) : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        PlacardSettingResponse.WritePlacardData(
            writer,
            placardId,
            ownerName,
            ownerAvatarId,
            tagId,
            slot,
            direction,
            tagName,
            position
        );
        return writer.ToBytes();
    }
}
