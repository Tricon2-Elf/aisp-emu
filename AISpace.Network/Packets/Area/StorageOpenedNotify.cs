using AISpace.Network;

namespace AISpace.Network.Packets.Area;

/// <summary>
/// recv_storage_opened (0x2CA5). Server-pushed; opens the account 倉庫 UI (PAS 1120).
/// 8 bytes: UInt64 deposit (piggy-bank balance). Purse AI points are tracked separately via money_updated_aipoint.
/// </summary>
public class StorageOpenedNotify(ulong deposit) : IOutgoingPacket
{
    public ulong Deposit = deposit;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(Deposit);
        return writer.ToBytes();
    }
}
