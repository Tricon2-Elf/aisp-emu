namespace AISpace.Network.Packets.Area;

/// <summary>recv_myroom_remove_furniture_r (0xFD30): four-byte result code.</summary>
public sealed class MyRoomRemoveFurnitureResponse(uint result) : IOutgoingPacket
{
    public uint Result { get; } = result;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(Result);
        return writer.ToBytes();
    }
}
