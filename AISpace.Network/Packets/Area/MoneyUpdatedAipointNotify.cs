using AISpace.Network;

namespace AISpace.Network.Packets.Area;

public sealed class MoneyUpdatedAipointNotify(ulong aiPoints) : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(aiPoints);
        return writer.ToBytes();
    }
}
