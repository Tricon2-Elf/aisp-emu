using aisp.Network;

namespace aisp.Network.Packets.Area;

/// <summary>
/// send_item_discard (0xED61). Bag context "捨てる": discard <see cref="Num"/> copies of the stack
/// whose serial is <see cref="SerialId"/> (serial == item id on this server).
/// Wire: UInt SerialId, UShort Num (6 bytes), per CProtoArea_client::send_item_discard(serialid, num).
/// </summary>
public sealed class ItemDiscardRequest : IIncomingPacket<ItemDiscardRequest>
{
    public uint SerialId { get; init; }
    public ushort Num { get; init; }

    public static ItemDiscardRequest FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        return new ItemDiscardRequest { SerialId = reader.ReadUInt(), Num = reader.ReadUShort() };
    }
}
