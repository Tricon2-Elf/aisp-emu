using aisp.Network;
using aisp.Network.Data;

namespace aisp.Network.Packets.Area;

/// <summary>
/// send_user_status_update (0xCF9A, wrapper 0x7B9F60): the status window's one-line text with its icon / colour
/// choice. u32 object id (the player's own avatar id), then the 53-byte status record (char[49] text, u32 icon).
/// The client copies the text out of a fixed buffer, so the bytes after the NUL are garbage.
/// </summary>
public sealed class UserStatusUpdateRequest(uint objectId, UserStatusData status)
    : IIncomingPacket<UserStatusUpdateRequest>
{
    public uint ObjectId { get; } = objectId;
    public UserStatusData Status { get; } = status;

    public static UserStatusUpdateRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        var objectId = reader.ReadUInt();
        var status = UserStatusData.FromBytes(reader.ReadBytes(UserStatusData.WireSize));
        return new UserStatusUpdateRequest(objectId, status);
    }
}
