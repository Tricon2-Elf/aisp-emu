using aisp.Network.Data;

namespace aisp.Network.Packets.Msg;

public class ItemGetBaseListResponse : IOutgoingPacket
{
    readonly uint _result;
    readonly List<ItemData> _items;

    public ItemGetBaseListResponse(uint result = 0, IEnumerable<ItemData>? items = null)
    {
        _result = result;
        _items = items is null ? [] : [.. items];
    }

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(_result);
        writer.Write((uint)_items.Count);
        foreach (var item in _items)
            writer.Write(item.ToBytes());
        return writer.ToBytes();
    }
}
