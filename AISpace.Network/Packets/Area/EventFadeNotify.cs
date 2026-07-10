using AISpace.Network;

namespace AISpace.Network.Packets.Area;

/// <summary>
/// recv_event_fade_in / recv_event_fade_out payload: float sec, byte r, byte g, byte b.
/// Script "fade-in" maps to mode that clears the overlay (target alpha 0);
/// "fade-out" maps to opaque overlay (target alpha 255).
/// </summary>
public sealed class EventFadeNotify(float seconds, byte r, byte g, byte b) : IOutgoingPacket
{
    public float Seconds { get; } = seconds;
    public byte R { get; } = r;
    public byte G { get; } = g;
    public byte B { get; } = b;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(Seconds);
        writer.Write(R);
        writer.Write(G);
        writer.Write(B);
        return writer.ToBytes();
    }
}
