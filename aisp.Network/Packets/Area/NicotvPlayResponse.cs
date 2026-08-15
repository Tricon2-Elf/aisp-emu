namespace aisp.Network.Packets.Area;

/// <summary>recv_nicotv_play_r (0x00E1): result + nicotvid.</summary>
public sealed class NicotvPlayResponse(uint result, uint nicotvId) : IOutgoingPacket
{
    public uint Result { get; } = result;
    public uint NicotvId { get; } = nicotvId;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(Result);
        writer.Write(NicotvId);
        return writer.ToBytes();
    }
}
