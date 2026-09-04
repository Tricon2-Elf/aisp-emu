using System.Numerics;

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
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(1u);
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
