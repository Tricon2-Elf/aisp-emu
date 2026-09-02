using aisp.Network;

namespace aisp.Network.Packets.Area;

/// <summary>send_adventure_work_delete (0x2DA5): UShort WorkId.</summary>
public sealed class AdventureWorkDeleteRequest(ushort workId)
    : IIncomingPacket<AdventureWorkDeleteRequest>
{
    public ushort WorkId { get; } = workId;

    public static AdventureWorkDeleteRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        return new AdventureWorkDeleteRequest(data.Length >= 2 ? reader.ReadUShort() : (ushort)0);
    }
}
