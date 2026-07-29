using AISpace.Network.Data;

namespace AISpace.Network.Packets.Area;

/// <summary>
/// recv_notify_room_list_pack (0xC0B2): count followed by at most ten
/// <see cref="RoomListEntry"/> records.
/// </summary>
public sealed class NotifyRoomListPack : IOutgoingPacket
{
    public const int MaximumRooms = 10;

    public NotifyRoomListPack(IReadOnlyList<RoomListEntry> rooms)
    {
        ArgumentNullException.ThrowIfNull(rooms);
        if (rooms.Count > MaximumRooms)
            throw new ArgumentOutOfRangeException(
                nameof(rooms),
                rooms.Count,
                $"The client accepts at most {MaximumRooms} rooms per packet."
            );

        Rooms = rooms;
    }

    public IReadOnlyList<RoomListEntry> Rooms { get; }

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write((uint)Rooms.Count);
        foreach (var room in Rooms)
            room.WriteTo(writer);

        return writer.ToBytes();
    }
}
