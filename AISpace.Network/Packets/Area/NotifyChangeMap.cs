using AISpace.Network;
using AISpace.Network.Data;

namespace AISpace.Network.Packets.Area;

/// <summary>
/// Server-to-client map change command (recv_notify_change_map).
/// Packet body:
/// UInt ChannelId
/// UInt MapId
/// UInt MapSerialId
/// UInt RouteState
/// Float SpawnX
/// Float SpawnY
/// Float SpawnZ
/// SByte Rotation
/// Byte Animation
/// Byte Flag
/// MessageServerInfo (UShort Port + Ascii[65] IP)
/// Byte FadeFlag
/// </summary>
public sealed class NotifyChangeMap : IOutgoingPacket
{
    public const int PacketSize = 99;

    public uint ChannelId { get; init; }
    public uint MapId { get; init; }
    public uint MapSerialId { get; init; }
    public uint RouteState { get; init; }
    public float PositionX { get; init; }
    public float PositionY { get; init; }
    public float PositionZ { get; init; }
    public sbyte Rotation { get; init; }
    public byte Animation { get; init; }
    public byte Flag { get; init; }
    public ServerInfo AreaServerInfo { get; init; } = new("0.0.0.0", 0);
    public byte FadeFlag { get; init; }

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(ChannelId);
        writer.Write(MapId);
        writer.Write(MapSerialId);
        writer.Write(RouteState);
        writer.Write(PositionX);
        writer.Write(PositionY);
        writer.Write(PositionZ);
        writer.Write(Rotation);
        writer.Write(Animation);
        writer.Write(Flag);
        writer.Write(AreaServerInfo.Port);
        writer.WriteFixedAsciiString(AreaServerInfo.IP, 65);
        writer.Write(FadeFlag);
        return writer.ToBytes();
    }

    public static NotifyChangeMap FromBytes(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        var channelId = reader.ReadUInt();
        var mapId = reader.ReadUInt();
        var mapSerialId = reader.ReadUInt();
        var routeState = reader.ReadUInt();
        var positionX = reader.ReadFloat();
        var positionY = reader.ReadFloat();
        var positionZ = reader.ReadFloat();
        var rotation = reader.ReadSByte();
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
}
