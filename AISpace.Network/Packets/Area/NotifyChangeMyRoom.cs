using AISpace.Network;
using AISpace.Network.Data;

namespace AISpace.Network.Packets.Area;

/// <summary>
/// recv_notify_change_myroom (0x0FA0), 174 bytes on the wire. Same role as NotifyChangeMap but additionally
/// puts the client into MyRoom mode (client handler CAIProtoArea_vtbl__func_37, aisp-decompiled.c:666331).
/// Wire layout (readers sub_798720 / ReadString_0x41 / sub_798840):
///   chrmap (30): UInt ChannelId, UInt MapId, UInt MapSerialId, UInt RouteState, Float X/Y/Z, SByte Rotation, Byte Animation
///   Byte  Flag            (bit 0x2 checked by transition handling)
///   MessageServerInfo (67): UShort Port, Ascii[65] IP
///   MyRoomInfo (75):
///     +0  UInt OwnerId           - room owner id; recv_notify_myroom_furniture entries must carry the same id
///     +4  UInt OwnerCharacterId  - compared against the local character id ("is my own room")
///     +8  UInt Unknown0
///     +12 Char[46] RoomName      (Shift-JIS)
///     +58 Byte RoomStage         - expansion stage 0-3, selects the settings/myroom.csv row
///     +59 UInt x4 Unknown1..4
///   Byte  FadeFlag
/// </summary>
public class NotifyChangeMyRoom : IOutgoingPacket
{
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
    public uint OwnerId { get; init; }
    public uint OwnerCharacterId { get; init; }
    public string RoomName { get; init; } = "My Room";
    public byte RoomStage { get; init; }
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

        writer.Write(OwnerId);
        writer.Write(OwnerCharacterId);
        writer.Write((uint)0);
        writer.WriteFixedJisString(RoomName, 46);
        writer.Write(RoomStage);
        writer.Write((uint)0);
        writer.Write((uint)0);
        writer.Write((uint)0);
        writer.Write((uint)0);

        writer.Write(FadeFlag);
        return writer.ToBytes();
    }
}
