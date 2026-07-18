using AISpace.Network;

namespace AISpace.Network.Packets.Area;

public readonly record struct FurnitureBaseEntry(uint ItemId, uint Flags, uint Unknown);

public class FurnitureGetBaseListResponse(uint result, IReadOnlyList<FurnitureBaseEntry> entries) : IOutgoingPacket
{
    public FurnitureGetBaseListResponse()
        : this(0, Array.Empty<FurnitureBaseEntry>()) { }

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(result);
        writer.Write((uint)entries.Count);
        foreach (var entry in entries)
        {
            writer.Write(entry.ItemId);
            writer.Write(entry.Flags);
            writer.Write(entry.Unknown);
        }

        return writer.ToBytes();
    }
}
