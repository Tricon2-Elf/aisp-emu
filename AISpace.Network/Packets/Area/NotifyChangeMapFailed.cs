using AISpace.Network;

namespace AISpace.Network.Packets.Area;

public sealed class NotifyChangeMapFailed(uint result) : IOutgoingPacket
{
    public uint Result { get; } = result;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(Result);
        return writer.ToBytes();
    }
}
