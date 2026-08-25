namespace aisp.Network.Data;

/// <summary>
/// One room entry consumed by <c>sub_798920</c> / <c>sub_4D1140</c>.
/// Wire layout is 92 bytes: roomId, UTF-8 name[46], owner[37], statusLo(1), status(4).
/// The client copies the trailing dword to internal offset +64 and uses that for the
/// status icon and Status-column sort; the displayed ルーム番号 is derived from RoomId.
/// </summary>
public sealed record RoomListEntry(uint RoomId, string RoomName, string OwnerName, uint Status)
{
    public const int RoomNameLength = 46;
    public const int OwnerNameLength = 37;
    public const int WireSize = 92;

    public void WriteTo(PacketWriter writer)
    {
        writer.Write(RoomId);
        writer.WriteFixedString(RoomName, RoomNameLength);
        writer.WriteFixedString(OwnerName, OwnerNameLength);
        // Low byte is stored at internal +60 (unused by list UI); dword at +64 drives icon/sort.
        writer.Write((byte)Status);
        writer.Write(Status);
    }
}
