using aisp.Network;
using aisp.Network.Data;

namespace aisp.Network.Packets.Area;

public sealed class FriendLinkTagGetResponse(
    uint result,
    uint avatarId,
    IReadOnlyList<FriendLinkTagData>? tagData = null,
    IReadOnlyList<uint>? slots = null,
    IReadOnlyList<FriendLinkTagData>? questionnaireTagData = null,
    IReadOnlyList<uint>? questionnaireSlots = null
) : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        PacketWriter writer = new();
        writer.Write(result);
        writer.Write(avatarId);
        WriteTags(writer, tagData);
        WriteSlots(writer, slots);
        WriteTags(writer, questionnaireTagData);
        WriteSlots(writer, questionnaireSlots);
        return writer.ToBytes();
    }

    private static void WriteTags(PacketWriter writer, IReadOnlyList<FriendLinkTagData>? tags)
    {
        var count = Math.Min(tags?.Count ?? 0, 5);
        writer.Write((uint)count);
        for (var i = 0; i < count; i++)
            tags![i].Write(writer);
    }

    private static void WriteSlots(PacketWriter writer, IReadOnlyList<uint>? slots)
    {
        var count = Math.Min(slots?.Count ?? 0, 5);
        writer.Write((uint)count);
        for (var i = 0; i < count; i++)
            writer.Write(slots![i]);
    }
}
