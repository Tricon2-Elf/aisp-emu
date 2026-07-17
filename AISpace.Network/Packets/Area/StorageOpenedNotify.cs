using AISpace.Network;

namespace AISpace.Network.Packets.Area;

/// <summary>
/// recv_storage_opened (0x2CA5). Server-pushed; opens the account 倉庫 UI (PAS 1120).
/// 8 bytes: UInt64 AiPoint (currency balance shown in the window).
/// </summary>
public class StorageOpenedNotify(ulong aiPoint) : IOutgoingPacket
{
    public ulong AiPoint = aiPoint;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(AiPoint);
        return writer.ToBytes();
    }
}
