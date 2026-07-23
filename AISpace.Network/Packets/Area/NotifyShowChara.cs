using AISpace.Network.Data;

namespace AISpace.Network.Packets.Area;

public sealed class NotifyShowChara(uint objectId, MovementData movement) : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(objectId);
        writer.Write(movement.ToBytes());
        return writer.ToBytes();
    }
}
