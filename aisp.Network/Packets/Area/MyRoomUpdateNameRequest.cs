namespace aisp.Network.Packets.Area;

/// <summary>send_myroom_update_name (0xB154): room ID followed by a null-terminated UTF-8 name of at most 46 bytes.</summary>
public sealed class MyRoomUpdateNameRequest(uint roomId, string name)
    : IIncomingPacket<MyRoomUpdateNameRequest>
{
    public const int MaximumEncodedNameBytes = 45;
    public const int MaximumPayloadSize = sizeof(uint) + MaximumEncodedNameBytes + 1;

    public uint RoomId { get; } = roomId;
    public string Name { get; } = name;

    public static MyRoomUpdateNameRequest FromBytes(ReadOnlySpan<byte> data)
    {
        if (data.Length < sizeof(uint) + 1 || data.Length > MaximumPayloadSize)
            throw new InvalidDataException(
                $"{nameof(MyRoomUpdateNameRequest)} requires a 4-byte room ID and a null-terminated name within a 46-byte field."
            );
        if (data[^1] != 0)
            throw new InvalidDataException(
                $"{nameof(MyRoomUpdateNameRequest)} name is not null terminated."
            );

        var reader = new PacketReader(data);
        return new MyRoomUpdateNameRequest(reader.ReadUInt(), reader.ReadString());
    }
}
