namespace AISpace.Network.Packets.Area;

/// <summary>send_myroom_update_security (0xE54D): room ID and eMYROOM_SECURITY value.</summary>
public sealed class MyRoomUpdateSecurityRequest(uint roomId, MyRoomSecurity security)
    : IIncomingPacket<MyRoomUpdateSecurityRequest>
{
    public const int WireSize = 8;

    public uint RoomId { get; } = roomId;
    public MyRoomSecurity Security { get; } = security;

    public static MyRoomUpdateSecurityRequest FromBytes(ReadOnlySpan<byte> data)
    {
        if (data.Length != WireSize)
            throw new InvalidDataException(
                $"{nameof(MyRoomUpdateSecurityRequest)} requires exactly {WireSize} bytes, received {data.Length}."
            );

        var reader = new PacketReader(data);
        return new MyRoomUpdateSecurityRequest(
            reader.ReadUInt(),
            (MyRoomSecurity)reader.ReadUInt()
        );
    }
}
