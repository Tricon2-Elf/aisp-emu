namespace aisp.Network.Packets.Msg;

/// <summary>Requests the comment history for a placard.</summary>
public sealed class GetPlacardCommentLogRequest(uint placardId)
    : IIncomingPacket<GetPlacardCommentLogRequest>
{
    public uint PlacardId { get; } = placardId;

    public static GetPlacardCommentLogRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        return new GetPlacardCommentLogRequest(reader.ReadUInt());
    }
}
