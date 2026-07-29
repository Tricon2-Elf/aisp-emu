namespace AISpace.Network.Packets.Area;

/// <summary>send_room_list_close (0x9A24): four-byte room ID.</summary>
public sealed class RoomListCloseRequest(uint roomId) : IIncomingPacket<RoomListCloseRequest>
{
    public const int WireSize = 4;

    public uint RoomId { get; } = roomId;

    public static RoomListCloseRequest FromBytes(ReadOnlySpan<byte> data)
    {
        if (data.Length != WireSize)
            throw new InvalidDataException(
                $"{nameof(RoomListCloseRequest)} requires exactly {WireSize} bytes, received {data.Length}."
            );

        return new RoomListCloseRequest(new PacketReader(data).ReadUInt());
    }
}
