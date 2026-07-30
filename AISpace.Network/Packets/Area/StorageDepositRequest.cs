namespace AISpace.Network.Packets.Area;

/// <summary>send_storage_deposit (0x51A4). 8 bytes: UInt64 AiPoint amount to deposit into 倉庫.</summary>
public sealed class StorageDepositRequest(ulong aiPoint) : IIncomingPacket<StorageDepositRequest>
{
    public const int WireSize = 8;

    public ulong AiPoint { get; } = aiPoint;

    public static StorageDepositRequest FromBytes(ReadOnlySpan<byte> data)
    {
        if (data.Length != WireSize)
            throw new InvalidDataException(
                $"{nameof(StorageDepositRequest)} requires exactly {WireSize} bytes, received {data.Length}."
            );

        return new StorageDepositRequest(new PacketReader(data).ReadULong());
    }
}
