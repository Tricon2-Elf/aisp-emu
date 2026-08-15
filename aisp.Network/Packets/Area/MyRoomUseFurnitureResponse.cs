using aisp.Network;

namespace aisp.Network.Packets.Area;

/// <summary>
/// recv_myroom_use_furniture_r (0xC437). 4 bytes: UInt Result (0 = ok).
/// </summary>
public class MyRoomUseFurnitureResponse(uint result) : IOutgoingPacket
{
    public uint Result = result;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(Result);
        return writer.ToBytes();
    }
}
