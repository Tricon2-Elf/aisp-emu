namespace AISpace.Network.Packets.Area;

public class NotifyDisappearChara(uint objId) : IOutgoingPacket
{
    public uint ObjId = objId;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(ObjId);
        writer.Write((uint)0); // ProduceID (usually 0)
        return writer.ToBytes();
    }
}
