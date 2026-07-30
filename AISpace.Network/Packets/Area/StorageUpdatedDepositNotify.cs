using AISpace.Network;

namespace AISpace.Network.Packets.Area;

/// <summary>recv_storage_updated_deposit (0xC515). 8 bytes: UInt64 deposit balance.</summary>
public sealed class StorageUpdatedDepositNotify(ulong deposit) : IOutgoingPacket
{
    public ulong Deposit { get; } = deposit;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(Deposit);
        return writer.ToBytes();
    }
}
