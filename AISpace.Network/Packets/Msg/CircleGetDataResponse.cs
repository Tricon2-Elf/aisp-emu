using AISpace.Network.Data;

namespace AISpace.Network.Packets.Msg;

public class CircleGetDataResponse(uint result, List<CircleData> circles) : IOutgoingPacket
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();

        // 1. Result (4 bytes)
        writer.Write(result);

        // 2. Count of Circles (4 bytes)
        writer.Write((uint)circles.Count);

        // 3. CircleData array (each 866 bytes)
        foreach (var c in circles)
        {
            writer.Write(c.ToBytes());
        }

        // 4. AuthLevel Count (4 bytes)
        writer.Write((uint)circles.Count);

        // 5. AuthLevel array (each 4 bytes)
        foreach (var c in circles)
        {
            writer.Write((uint)1); // 1 = Leader
        }

        // THERE SHOULD BE NO PADDING AT THE END OF THE PACKET!
        return writer.ToBytes();
    }
}
