using AISpace.Network;

namespace AISpace.Network.Packets.Msg;

public class AvatarCreateResponse(uint Result) : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(Result);
        return writer.ToBytes();
    }
}
