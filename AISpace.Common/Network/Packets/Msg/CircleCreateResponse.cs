using AISpace.Common.Game;

namespace AISpace.Common.Network.Packets.Msg;

public class CircleCreateResponse(uint result, CircleData? data) : IPacket<CircleCreateResponse>
{
    public static CircleCreateResponse FromBytes(ReadOnlySpan<byte> data) => throw new NotImplementedException();

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
