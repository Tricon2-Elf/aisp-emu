using aisp.Network;

namespace aisp.Network.Packets.Area;

/// <summary>send_adventure_upload_end (0xB592): the drama upload window (ドラマショップ 買取) is being closed. No body.</summary>
public sealed class AdventureUploadEndRequest(byte[] raw)
    : IIncomingPacket<AdventureUploadEndRequest>
{
    public byte[] Raw { get; } = raw;

    public static AdventureUploadEndRequest FromBytes(ReadOnlySpan<byte> data) =>
        new(data.ToArray());
}
