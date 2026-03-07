using AISpace.Network;

namespace AISpace.Network.Packets.Common;

public class LogoutResponse : IPacket<LogoutResponse>
{
    public static LogoutResponse FromBytes(ReadOnlySpan<byte> data)
    {
        throw new NotImplementedException();
    }

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(0);
        return writer.ToBytes();
    }
}
