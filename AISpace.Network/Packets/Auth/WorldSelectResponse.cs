using AISpace.Network;

namespace AISpace.Network.Packets.Auth;

public class WorldSelectResponse(uint result, string ipAddress, ushort port, string otp)
    : IOutgoingPacket
{
    uint worldCount = 1;
    public string IpAddress = ipAddress;
    public ushort Port = port;
    public string OTP = otp;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(result); //Result and World count
        writer.Write(worldCount);
        writer.Write(Port);
        writer.WriteFixedAsciiString(IpAddress, 65);
        writer.WriteFixedAsciiString(OTP, 20);
        return writer.ToBytes();
    }
}
