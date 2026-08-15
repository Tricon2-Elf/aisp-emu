using aisp.Network;

namespace aisp.Network.Packets.Area;

public class ItemTryEquipReplaceResponse(uint result) : IOutgoingPacket
{
    public uint Result { get; set; } = result;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(Result);
        return writer.ToBytes();
    }
}
