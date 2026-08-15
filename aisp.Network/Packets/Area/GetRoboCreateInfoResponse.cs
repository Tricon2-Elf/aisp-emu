using aisp.Network.Data;

namespace aisp.Network.Packets.Area;

public sealed class GetRoboCreateInfoResponse(
    uint modelId,
    uint hairstyle,
    IReadOnlyList<ItemSlotInfo> equips
) : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(modelId);
        writer.Write(hairstyle);
        writer.Write((uint)equips.Count);
        foreach (var equip in equips)
            writer.Write(equip.ToBytes());
        return writer.ToBytes();
    }
}
