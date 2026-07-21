using AISpace.Network;

namespace AISpace.Network.Packets.Area;

public sealed class MonsterGetDataResponse(uint result = 0) : IOutgoingPacket
{
    public uint Result { get; } = result;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(Result);
        return writer.ToBytes();
    }
}
