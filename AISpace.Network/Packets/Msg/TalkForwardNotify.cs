using AISpace.Network;

namespace AISpace.Network.Packets.Msg;

public class TalkForwardNotify(uint fromId, uint distId, string message, uint balloonId) : IOutgoingPacket
{
    public uint FromId = fromId;
    public uint DistId = distId;
    public string Message = message;
    public uint BalloonId = balloonId;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(FromId);
        writer.Write(DistId);
        writer.Write(Message, "utf-8");
        writer.Write(BalloonId);
        return writer.ToBytes();
    }
}
