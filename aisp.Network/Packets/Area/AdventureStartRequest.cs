using aisp.Network;

namespace aisp.Network.Packets.Area;

/// <summary>send_adventure_start (0x2939): sent by the drama player. Body not interpreted here.</summary>
public sealed class AdventureStartRequest(byte[] raw) : IIncomingPacket<AdventureStartRequest>
{
    public byte[] Raw { get; } = raw;

    public static AdventureStartRequest FromBytes(ReadOnlySpan<byte> data) => new(data.ToArray());
}
