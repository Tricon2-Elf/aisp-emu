using AISpace.Network;

namespace AISpace.Network.Packets.Area;

public class ItemTryEquipResetResponse(uint result) : IOutgoingPacket
{
    public uint Result = result;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(Result);
        return writer.ToBytes();
    }
}
