namespace AISpace.Network.Packets.Area;

/// <summary>
/// send_get_myroom_furniture (0xE868), 8-byte payload: map ID and channel ID.
/// The client sends this during every area-map load, not only while entering a MyRoom map.
/// </summary>
public sealed class MyRoomGetFurnitureRequest(uint mapId, uint channelId) : IIncomingPacket<MyRoomGetFurnitureRequest>
{
    public const int WireSize = 8;

    public uint MapId { get; } = mapId;
    public uint ChannelId { get; } = channelId;

    public static MyRoomGetFurnitureRequest FromBytes(ReadOnlySpan<byte> data)
    {
        if (data.Length != WireSize)
            throw new InvalidDataException($"{nameof(MyRoomGetFurnitureRequest)} requires exactly {WireSize} bytes, received {data.Length}.");

        var reader = new PacketReader(data);
        return new MyRoomGetFurnitureRequest(reader.ReadUInt(), reader.ReadUInt());
    }
}
