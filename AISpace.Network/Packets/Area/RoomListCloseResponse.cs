namespace AISpace.Network.Packets.Area;

/// <summary>recv_room_list_close_r (0xCBE8): four-byte result code.</summary>
public sealed class RoomListCloseResponse(uint result) : IOutgoingPacket
{
    public uint Result { get; } = result;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(Result);
        return writer.ToBytes();
    }
}
