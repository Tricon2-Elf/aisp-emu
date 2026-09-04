using System.Numerics;

namespace aisp.Network.Packets.Area;

/// <summary>Placard-setting result followed by the 127-byte wire placard record.</summary>
public sealed class PlacardSettingResponse(
    uint result,
    uint placardId = 0,
    string ownerName = "",
    uint ownerAvatarId = 0,
    uint tagId = 0,
    uint slot = 0,
    byte direction = 0,
    string tagName = "",
    Vector3 position = default
) : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(result);
        WritePlacardData(
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

    internal static void WritePlacardData(
        PacketWriter writer,
        uint placardId,
        string ownerName,
        uint ownerAvatarId,
        uint tagId,
        uint slot,
        byte direction,
        string tagName,
        Vector3 position
    )
    {
        writer.Write(placardId);
        writer.WriteFixedString(ownerName, 37);
        writer.Write(position.X);
        writer.Write(position.Y);
        writer.Write(position.Z);
        writer.Write(direction);
        writer.Write(ownerAvatarId);
        writer.Write(tagId);
        writer.WriteFixedString(tagName, 61);
        writer.Write(slot);
    }
}
