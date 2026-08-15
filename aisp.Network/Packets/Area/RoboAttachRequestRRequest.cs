namespace aisp.Network.Packets.Area;

/// <summary>
/// Client reply after processing <see cref="RoboAttachRequestNotify"/>.
/// Payload: UInt RoboId + UInt Result.
/// </summary>
public sealed class RoboAttachRequestRRequest : IIncomingPacket<RoboAttachRequestRRequest>
{
    public uint RoboId { get; init; }
    public uint Result { get; init; }

    public static RoboAttachRequestRRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        return new RoboAttachRequestRRequest
        {
            RoboId = reader.ReadUInt(),
            Result = reader.ReadUInt(),
        };
    }
}
