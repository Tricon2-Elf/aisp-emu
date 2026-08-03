using AISpace.Network;

namespace AISpace.Network.Packets.Area;

/// <summary>
/// recv_notify_myhouse_change_security (0x8F88). Broadcast room security change.
/// Payload: UInt houseid (room id), UInt security (eMYROOM_SECURITY).
/// </summary>
public sealed class NotifyMyHouseChangeSecurity(uint houseId, MyRoomSecurity security)
    : IOutgoingPacket
{
    public uint HouseId { get; } = houseId;
    public MyRoomSecurity Security { get; } = security;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(HouseId);
        writer.Write((uint)Security);
        return writer.ToBytes();
    }
}
