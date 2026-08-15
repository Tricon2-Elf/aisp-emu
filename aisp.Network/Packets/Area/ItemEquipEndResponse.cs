using aisp.Network;

namespace aisp.Network.Packets.Area;

public class ItemEquipEndResponse : IOutgoingPacket
{
    public uint Result { get; set; }

    public ItemEquipEndResponse(uint result)
    {
        Result = result;
    }

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(Result);
        return writer.ToBytes();
    }
}
