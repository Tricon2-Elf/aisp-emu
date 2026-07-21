namespace AISpace.Network.Packets.Area;

/// <summary>
/// Asks the client whether its TPS controller is active. The client answers with
/// <see cref="EventGetTpsModeRequest"/>.
/// </summary>
public sealed class EventGetTpsModeNotify : IOutgoingPacket
{
    public byte[] ToBytes() => [];
}
