namespace AISpace.Common.Network.Packets.Area;

/// <summary>
/// Server→Client: tells the client to remove a character/avatar from the scene.
/// Protocol: recv_notify_disappear_chara (0xD3A4).
/// </summary>
public record NotifyDisappearChara(uint ObjId, uint ProduceId) : IPacket<NotifyDisappearChara>
{
    public static NotifyDisappearChara FromBytes(ReadOnlySpan<byte> data)
    {
        throw new NotImplementedException();
    }

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(ObjId);
        writer.Write(ProduceId);
        return writer.ToBytes();
    }
}
