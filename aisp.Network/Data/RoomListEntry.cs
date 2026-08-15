namespace aisp.Network.Data;

/// <summary>
/// One room entry consumed by <c>sub_798920</c>. The client stores each entry in a
/// 92-byte structure with fixed Shift-JIS room and owner names.
/// </summary>
public sealed record RoomListEntry(
    uint RoomId,
    string RoomName,
    string OwnerName,
    byte Status,
    uint RoomNumber
)
{
    public const int RoomNameLength = 46;
    public const int OwnerNameLength = 37;
    public const int WireSize = 92;

    public void WriteTo(PacketWriter writer)
    {
        writer.Write(RoomId);
        writer.WriteFixedJisString(RoomName, RoomNameLength);
        writer.WriteFixedJisString(OwnerName, OwnerNameLength);
        writer.Write(Status);
        writer.Write(RoomNumber);
    }
}
