namespace aisp.Network.Data;

/// <summary>
/// The client protocol's <c>chrmap</c> value. The same layout is used by
/// avatar data, robot state updates, and map-change notifications.
/// </summary>
public sealed class CharacterMapData
{
    public const int WireSize = 30;

    public uint ChannelId { get; set; }
    public uint MapId { get; set; }
    public uint MapSerialId { get; set; }
    public uint RouteState { get; set; }
    public MovementData Movement { get; set; } = new(0, 0, 0, 0, MovementType.Stopped);

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(ChannelId);
        writer.Write(MapId);
        writer.Write(MapSerialId);
        writer.Write(RouteState);
        writer.Write(Movement.ToBytes());
        return writer.ToBytes();
    }

    public static CharacterMapData FromBytes(ReadOnlySpan<byte> data)
    {
        if (data.Length < WireSize)
            throw new ArgumentException(
                $"CharacterMapData requires at least {WireSize} bytes.",
                nameof(data)
            );

        var reader = new PacketReader(data);
        return new CharacterMapData
        {
            ChannelId = reader.ReadUInt(),
            MapId = reader.ReadUInt(),
            MapSerialId = reader.ReadUInt(),
            RouteState = reader.ReadUInt(),
            Movement = MovementData.FromBytes(reader.ReadBytes(14)),
        };
    }
}
