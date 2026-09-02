using aisp.Network;

namespace aisp.Network.Packets.Area;

/// <summary>send_adventure_end (0x2125): sent by the drama player. Body not interpreted here.</summary>
public sealed class AdventureEndRequest(byte[] raw) : IIncomingPacket<AdventureEndRequest>
{
    public byte[] Raw { get; } = raw;

    public static AdventureEndRequest FromBytes(ReadOnlySpan<byte> data) => new(data.ToArray());
}
