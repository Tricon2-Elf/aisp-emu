using AISpace.Network;

namespace AISpace.Network.Packets.Area;

/// <summary>
/// recv_notify_myroom_furniture (0xA64A). 34-byte wire furniture struct, parsed by the client's
/// furniture reader (sub_7989B0, aisp-decompiled.c:679210) and consumed by sub_48AC50 (:107151):
///   UInt  OwnerId       - must match dword 0 of the myroom info sent in NotifyChangeMyRoom, otherwise ignored
///   UInt  SerialId      - furniture serial (echoed by send_myroom_use_furniture)
///   UInt  ActionType    - wire field; spawn ignores it for click UI (see furniture.csv アクション on ItemId)
///   UInt  ItemId        - model + click via furniture.csv (1=drama play, 2=drama work list/13000, 3=TV, 4=use_furniture→server)
///   Float X, Y, Z
///   Byte  Yaw           - degrees / 2 (client: rad = byte * PI/180 * 2)
///   Byte  Pitch         - degrees / 2
///   UInt  ActiveFlag    - 1 = interactable/shown
/// The client only processes this while in MyRoom mode (set by recv_notify_change_myroom).
/// </summary>
public class MyRoomNotifyFurniture(uint ownerId, uint serialId, uint actionType, uint itemId, float x, float y, float z, byte yawHalfDegrees = 0, byte pitchHalfDegrees = 0, bool active = true) : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(ownerId);
        writer.Write(serialId);
        writer.Write(actionType);
        writer.Write(itemId);
        writer.Write(x);
        writer.Write(y);
        writer.Write(z);
        writer.Write(yawHalfDegrees);
        writer.Write(pitchHalfDegrees);
        writer.Write(active ? 1u : 0u);
        return writer.ToBytes();
    }
}
