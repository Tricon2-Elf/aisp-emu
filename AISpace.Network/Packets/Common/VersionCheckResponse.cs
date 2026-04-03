using AISpace.Network;

namespace AISpace.Network.Packets.Common;

public class VersionCheckResponse(uint Result, uint Major, uint Minor, uint Ver) : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(Result);
        writer.Write(Major);
        writer.Write(Minor);
        writer.Write(Ver);
        return writer.ToBytes();
    }
}
