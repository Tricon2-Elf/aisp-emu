using AISpace.Network.Data;

namespace AISpace.Network.Packets.Msg;

public class CircleCreateResponse(uint result, CircleData? data) : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(result); // 4 bytes

        if (data != null && result == 0)
        {
            writer.Write(data.ToBytes()); // 866 bytes
        }
        else
        {
            // If error, write dummy 866 bytes to avoid breaking the parser
            writer.Write(new byte[866]);
        }

        return writer.ToBytes();
    }
}
