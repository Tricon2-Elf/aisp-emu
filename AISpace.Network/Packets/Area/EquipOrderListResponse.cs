using AISpace.Network.Data;

namespace AISpace.Network.Packets.Area;

public class EquipOrderListResponse : IOutgoingPacket
{
    public uint Result { get; set; }
    public IReadOnlyList<CharaOrderData> CharaOrders { get; set; } = CharaOrderData.DefaultClothingOrders;

    public EquipOrderListResponse(uint result = 0)
    {
        Result = result;
    }

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(Result);
        writer.Write((uint)CharaOrders.Count);
        foreach (var order in CharaOrders)
            writer.Write(order.ToBytes());
        writer.Write(0u); // job order count
        return writer.ToBytes();
    }
}
