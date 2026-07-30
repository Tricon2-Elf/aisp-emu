namespace AISpace.Network.Packets.Area;

/// <summary>send_storage_withdraw (0x9C26). 8 bytes: UInt64 AiPoint amount to withdraw from 倉庫.</summary>
public sealed class StorageWithdrawRequest(ulong aiPoint) : IIncomingPacket<StorageWithdrawRequest>
{
    public const int WireSize = 8;

    public ulong AiPoint { get; } = aiPoint;

    public static StorageWithdrawRequest FromBytes(ReadOnlySpan<byte> data)
    {
        if (data.Length != WireSize)
            throw new InvalidDataException(
                $"{nameof(StorageWithdrawRequest)} requires exactly {WireSize} bytes, received {data.Length}."
            );

        return new StorageWithdrawRequest(new PacketReader(data).ReadULong());
    }
}
