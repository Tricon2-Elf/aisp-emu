using AISpace.Network;

namespace AISpace.Network.Packets.Auth;

public class AuthenticateResponse(uint id) : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(id);
        return writer.ToBytes();
    }
}
