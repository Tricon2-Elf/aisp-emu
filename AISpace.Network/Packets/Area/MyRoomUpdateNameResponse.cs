namespace AISpace.Network.Packets.Area;

/// <summary>recv_myroom_update_name_r (0xB186): four-byte result code.</summary>
public sealed class MyRoomUpdateNameResponse(uint result) : IOutgoingPacket
{
    public uint Result { get; } = result;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(Result);
        return writer.ToBytes();
    }
}
