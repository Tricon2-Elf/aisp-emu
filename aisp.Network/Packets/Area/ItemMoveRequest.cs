namespace aisp.Network.Packets.Area;

/// <summary>
/// send_item_move (0x8C7C). Moves stacks between item-table places.
/// Wardrobe warehouse uses place 0 (inventory) ↔ place 1 (account storage).
/// Wire: UInt FromPlace, UInt SerialId, UShort Num, UInt ToPlace, UInt TargetId (18 bytes).
/// </summary>
public sealed class ItemMoveRequest : IIncomingPacket<ItemMoveRequest>
{
    public const int WireSize = 18;

    public required uint FromPlace { get; init; }
    public required uint SerialId { get; init; }
    public required ushort Num { get; init; }
    public required uint ToPlace { get; init; }
    public required uint TargetId { get; init; }

    public static ItemMoveRequest FromBytes(ReadOnlySpan<byte> data)
    {
        if (data.Length != WireSize)
            throw new InvalidDataException(
                $"{nameof(ItemMoveRequest)} requires exactly {WireSize} bytes, received {data.Length}."
            );

        var reader = new PacketReader(data);
        return new ItemMoveRequest
        {
            FromPlace = reader.ReadUInt(),
            SerialId = reader.ReadUInt(),
            Num = reader.ReadUShort(),
            ToPlace = reader.ReadUInt(),
            TargetId = reader.ReadUInt(),
        };
    }
}
