using AISpace.Network;

namespace AISpace.Network.Packets.Area;

public class NotifySupplyNpcExec(uint objId) : IPacket<NotifySupplyNpcExec>
{
    public static NotifySupplyNpcExec FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        return new NotifySupplyNpcExec(reader.ReadUInt());
    }

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(objId);
        return writer.ToBytes();
    }
}
