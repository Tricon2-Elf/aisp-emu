using aisp.Network;

namespace aisp.Network.Packets.Area;

/// <summary>send_adventure_work_create (0xB1D9): UInt Sheets — manuscript sheets for the new work (the editor sends 1).</summary>
public sealed class AdventureWorkCreateRequest(uint sheets)
    : IIncomingPacket<AdventureWorkCreateRequest>
{
    public uint Sheets { get; } = sheets;

    public static AdventureWorkCreateRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        return new AdventureWorkCreateRequest(data.Length >= 4 ? reader.ReadUInt() : 1u);
    }
}
