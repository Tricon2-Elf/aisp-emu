using aisp.Network.Data;

namespace aisp.Network.Packets.Area;

public sealed class UccVoiceBaseListResponse(IReadOnlyList<UccVoice>? voices = null)
    : IOutgoingPacket
{
    public const int MaximumEntryCount = 1500;

    public byte[] ToBytes()
    {
        if (voices?.Count > MaximumEntryCount)
            throw new InvalidOperationException("Too many voice definitions.");
        var writer = new PacketWriter();
        writer.Write(0u);
        writer.Write((uint)(voices?.Count ?? 0));
        if (voices is not null)
            foreach (var voice in voices)
                voice.Write(writer);
        return writer.ToBytes();
    }
}
