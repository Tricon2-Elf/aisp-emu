using AISpace.Network;

namespace AISpace.Common.Handlers.Area;

public class NotifyDisappearChara(uint objId) : IPacket<NotifyDisappearChara>
{
    public uint ObjId = objId;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(ObjId);
        writer.Write((uint)0); // ProduceID (usually 0)
        return writer.ToBytes();
    }

    public static NotifyDisappearChara FromBytes(ReadOnlySpan<byte> data) => throw new NotImplementedException();
}
