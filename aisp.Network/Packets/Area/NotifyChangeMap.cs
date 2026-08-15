using aisp.Network;
using aisp.Network.Data;

namespace aisp.Network.Packets.Area;

/// <summary>
/// Server-to-client map change command (recv_notify_change_map).
/// <see cref="Rotation"/> is degrees; written as wire half-degrees.
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

    /// <summary>Facing in degrees.</summary>
    public int Rotation { get; init; }

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
        writer.Write(YawEncoding.ToWireSByte(Rotation));
        writer.Write(Animation);
        writer.Write(Flag);
        writer.Write(AreaServerInfo.Port);
        writer.WriteFixedAsciiString(AreaServerInfo.IP, 65);
        writer.Write(FadeFlag);
        return writer.ToBytes();
    }
}
