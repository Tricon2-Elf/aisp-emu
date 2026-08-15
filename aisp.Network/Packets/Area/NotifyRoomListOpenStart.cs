namespace aisp.Network.Packets.Area;

/// <summary>recv_notify_room_list_open_start (0xA5BA): empty payload.</summary>
public sealed class NotifyRoomListOpenStart : IOutgoingPacket
{
    public byte[] ToBytes() => [];
}
