namespace AISpace.Network.Packets.Area;

/// <summary>recv_notify_room_list_open_end (0xDC32): empty payload.</summary>
public sealed class NotifyRoomListOpenEnd : IOutgoingPacket
{
    public byte[] ToBytes() => [];
}
