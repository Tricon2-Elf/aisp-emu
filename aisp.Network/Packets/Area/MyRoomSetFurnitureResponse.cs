namespace aisp.Network.Packets.Area;

/// <summary>recv_myroom_set_furniture_r (0x1840): four-byte result code.</summary>
public sealed class MyRoomSetFurnitureResponse(uint result) : IOutgoingPacket
{
    public uint Result { get; } = result;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(Result);
        return writer.ToBytes();
    }
}
