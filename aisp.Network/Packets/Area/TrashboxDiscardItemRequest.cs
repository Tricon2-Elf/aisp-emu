using aisp.Network;

namespace aisp.Network.Packets.Area;

/// <summary>
/// send_trashbox_discard_item (0xB18E). Sent by the trashbox window with every stack the player
/// dropped into the bin: UInt count1 (max 10), UInt serialIds[count1], UInt count2 (max 10),
/// UShort nums[count2]. The client fills both arrays from the same selection list.
/// </summary>
public sealed class TrashboxDiscardItemRequest : IIncomingPacket<TrashboxDiscardItemRequest>
{
    public const int MaxStacks = 10;

    public IReadOnlyList<uint> SerialIds { get; init; } = [];
    public IReadOnlyList<ushort> Nums { get; init; } = [];

    public static TrashboxDiscardItemRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        var serialCount = reader.ReadUInt();
        if (serialCount > MaxStacks)
            throw new InvalidDataException(
                $"trashbox serial count {serialCount} exceeds {MaxStacks}"
            );
        var serials = new uint[serialCount];
        for (var i = 0; i < serials.Length; i++)
            serials[i] = reader.ReadUInt();

        var numCount = reader.ReadUInt();
        if (numCount > MaxStacks)
            throw new InvalidDataException($"trashbox num count {numCount} exceeds {MaxStacks}");
        var nums = new ushort[numCount];
        for (var i = 0; i < nums.Length; i++)
            nums[i] = reader.ReadUShort();

        return new TrashboxDiscardItemRequest { SerialIds = serials, Nums = nums };
    }
}
