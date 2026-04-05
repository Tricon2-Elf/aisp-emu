using AISpace.Network;

namespace AISpace.Network.Packets.Area;

public class TrashboxOpenResponse(uint result) : IOutgoingPacket
{
    public uint Result { get; set; } = result;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(Result);
        return writer.ToBytes();
    }
}
