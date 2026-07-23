using AISpace.Network;
using AISpace.Network.Data;
using AISpace.Network.Packets.Area;

namespace AISpace.Network.Tests;

/// <summary>Parsing helpers for server-originated payloads used only by tests.</summary>
internal static class OutgoingPacketTestParsers
{
    public static NotifyChangeMap ParseNotifyChangeMap(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        var channelId = reader.ReadUInt();
        var mapId = reader.ReadUInt();
        var mapSerialId = reader.ReadUInt();
        var routeState = reader.ReadUInt();
        var positionX = reader.ReadFloat();
        var positionY = reader.ReadFloat();
        var positionZ = reader.ReadFloat();
        var rotation = YawEncoding.FromWireSByte(reader.ReadSByte());
        var animation = reader.ReadByte();
        var flag = reader.ReadByte();
        var port = reader.ReadUShort();
        var ip = reader.ReadFixedString(65, "ASCII");
        var fadeFlag = reader.ReadByte();

        return new NotifyChangeMap
        {
            ChannelId = channelId,
            MapId = mapId,
            MapSerialId = mapSerialId,
            RouteState = routeState,
            PositionX = positionX,
            PositionY = positionY,
            PositionZ = positionZ,
            Rotation = rotation,
            Animation = animation,
            Flag = flag,
            AreaServerInfo = new ServerInfo(ip, port),
            FadeFlag = fadeFlag,
        };
    }

    public static EventAreaMapSelectExecNotify ParseEventAreaMapSelectExecNotify(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        var count = reader.ReadUInt();
        if (count > 4)
            throw new InvalidDataException($"Area map selection count {count} exceeds client maximum of 4.");

        var entries = new List<NotifySelectMapEntry>((int)count);
        for (var index = 0; index < count; index++)
            entries.Add(NotifySelectMapEntry.FromBytes(reader.ReadBytes(NotifySelectMapEntry.PacketSize)));

        return new EventAreaMapSelectExecNotify
        {
            Entries = entries,
            IslandId = reader.ReadUInt(),
            IsRegisteredIsland = reader.ReadUInt(),
        };
    }

    public static EventAreaMapSelectCloseNotify ParseEventAreaMapSelectCloseNotify(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        return new EventAreaMapSelectCloseNotify(reader.ReadUInt());
    }

    public static SelectInitIslandStartNotify ParseSelectInitIslandStartNotify(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        var count = reader.ReadUInt();
        if (count > 5)
            throw new InvalidDataException($"Island bootstrap count {count} exceeds client maximum of 5.");

        var islands = new List<SelectInitIslandEntry>((int)count);
        for (var index = 0; index < count; index++)
            islands.Add(SelectInitIslandEntry.FromBytes(reader.ReadBytes(SelectInitIslandEntry.PacketSize)));

        return new SelectInitIslandStartNotify { Islands = islands };
    }

    public static EventIslandSelectExecNotify ParseEventIslandSelectExecNotify(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        var count = reader.ReadUInt();
        if (count > 5)
            throw new InvalidDataException($"Event island selection count {count} exceeds client maximum of 5.");

        var islands = new List<EventIslandSelectEntry>((int)count);
        for (var index = 0; index < count; index++)
            islands.Add(EventIslandSelectEntry.FromBytes(reader.ReadBytes(EventIslandSelectEntry.PacketSize)));

        return new EventIslandSelectExecNotify { Islands = islands };
    }

    public static MapLinkNotifyData ParseMapLinkNotifyData(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        var result = reader.ReadUInt();
        var mapLinkData = new MapLinkData
        {
            PositionX = reader.ReadFloat(),
            PositionY = reader.ReadFloat(),
            PositionZ = reader.ReadFloat(),
            Yaw = reader.ReadByte(),
            Length = reader.ReadFloat(),
            Depth = reader.ReadFloat(),
        };
        return new MapLinkNotifyData(result, mapLinkData);
    }

    public static byte[] EventAreaMapSelectExecRRequestToBytes(EventAreaMapSelectExecRRequest p)
    {
        var writer = new PacketWriter();
        writer.Write(p.Result);
        writer.Write(p.MapId);
        writer.Write(p.ChannelId);
        return writer.ToBytes();
    }

    public static byte[] SelectInitIslandEndRequestToBytes(SelectInitIslandEndRequest p)
    {
        var writer = new PacketWriter();
        writer.Write(p.IslandId);
        return writer.ToBytes();
    }
}
