using AISpace.Network;

namespace AISpace.Network.Packets.Area;

/// <summary>
/// recv_myroom_end_furniture_r (0xCECA), 4 bytes: UInt Result.
/// </summary>
public class MyRoomEndFurnitureResponse(uint result) : IOutgoingPacket
{
    public uint Result = result;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(Result);
        return writer.ToBytes();
    }
}
