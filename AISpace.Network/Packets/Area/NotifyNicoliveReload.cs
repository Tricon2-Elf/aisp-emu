using System.Text;

namespace AISpace.Network.Packets.Area;

/// <summary>recv_notify_nicolive_reload (0xE342): supplies a null-terminated Nico Live program ID.</summary>
public sealed class NotifyNicoliveReload(string liveId) : IOutgoingPacket
{
    public const int MaximumEncodedLiveIdBytes = 96;

    public string LiveId { get; } = liveId;

    public byte[] ToBytes()
    {
        if (!LiveId.All(char.IsAscii))
            throw new InvalidOperationException(
                $"{nameof(LiveId)} must contain only ASCII characters."
            );

        var encodedLength = Encoding.ASCII.GetByteCount(LiveId);
        if (encodedLength > MaximumEncodedLiveIdBytes)
            throw new InvalidOperationException(
                $"{nameof(LiveId)} cannot exceed {MaximumEncodedLiveIdBytes} encoded bytes."
            );

        var writer = new PacketWriter();
        writer.Write(LiveId);
        return writer.ToBytes();
    }
}
