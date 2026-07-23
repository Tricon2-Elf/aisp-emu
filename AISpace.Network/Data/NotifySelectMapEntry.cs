using AISpace.Network;

namespace AISpace.Network.Data;

/// <summary>
/// One select_map_t entry for recv_notify_select_map.
/// Decompiled parsing aligns with:
/// UInt MapId + 67-byte MessageServerInfo + 30-byte packed route/mapdata + UInt trailing + UInt trailing.
/// </summary>
public sealed class NotifySelectMapEntry
{
    public const int PacketSize = 109;

    public uint MapId { get; init; }
    public ServerInfo AreaServerInfo { get; init; } = new("0.0.0.0", 0);
    public uint ChannelId { get; init; }
    public uint RouteMapId { get; init; }
    public uint MapSerialId { get; init; }
    public uint RouteState { get; init; }
    public float PositionX { get; init; }
    public float PositionY { get; init; }
    public float PositionZ { get; init; }

    /// <summary>Facing in degrees; written as wire half-degrees.</summary>
    public int Yaw { get; init; }

    public byte Animation { get; init; }
    public uint Unknown1 { get; init; }
    public uint Unknown2 { get; init; }

    public void WriteTo(PacketWriter writer)
    {
        writer.Write(MapId);
        writer.Write(AreaServerInfo.Port);
        writer.WriteFixedAsciiString(AreaServerInfo.IP, 65);

        writer.Write(ChannelId);
        writer.Write(RouteMapId != 0 ? RouteMapId : MapId);
        writer.Write(MapSerialId);
        writer.Write(RouteState);
        writer.Write(PositionX);
        writer.Write(PositionY);
        writer.Write(PositionZ);
        writer.Write(YawEncoding.ToWireByte(Yaw));
        writer.Write(Animation);

        writer.Write(Unknown1);
        writer.Write(Unknown2);
    }

    public static NotifySelectMapEntry FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        var mapId = reader.ReadUInt();
        var areaServerPort = reader.ReadUShort();
        var areaServerIp = reader.ReadFixedString(65, "ASCII");

        return new NotifySelectMapEntry
        {
            MapId = mapId,
            AreaServerInfo = new ServerInfo(areaServerIp, areaServerPort),
            ChannelId = reader.ReadUInt(),
            RouteMapId = reader.ReadUInt(),
            MapSerialId = reader.ReadUInt(),
            RouteState = reader.ReadUInt(),
            PositionX = reader.ReadFloat(),
            PositionY = reader.ReadFloat(),
            PositionZ = reader.ReadFloat(),
            Yaw = YawEncoding.FromWireByte(reader.ReadByte()),
            Animation = reader.ReadByte(),
            Unknown1 = reader.ReadUInt(),
            Unknown2 = reader.ReadUInt(),
        };
    }
}
