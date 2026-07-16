using AISpace.Network;

namespace AISpace.Network.Packets.Area;

/// <summary>
/// recv_myroom_start_furniture_r (0x19BC), 8 bytes: UInt Result, UInt MaxPlacementCount.
/// </summary>
public class MyRoomStartFurnitureResponse(uint result, uint maxPlacementCount) : IOutgoingPacket
{
    public uint Result = result;
    public uint MaxPlacementCount = maxPlacementCount;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(Result);
        writer.Write(MaxPlacementCount);
        return writer.ToBytes();
    }
}
