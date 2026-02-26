using AISpace.Common.Game;

namespace AISpace.Common.Network.Packets.Msg;

public class ChannelSelectMyRoomResponse(uint result, ServerInfo serverInfo, uint mapId, uint mapSerialId) : IPacket<ChannelSelectMyRoomResponse>
{
    public byte[] ToBytes()
    {
        var writer = new PacketWriter();

        writer.Write(result);
        writer.Write(serverInfo.Port);
        writer.WriteFixedAsciiString(serverInfo.IP, 65);
        writer.Write((byte)0);
        writer.Write(mapId);
        writer.Write(mapSerialId);
        writer.Write((uint)0);
        writer.Write((uint)0);
        writer.Write((uint)0);
        writer.Write(mapId);
        writer.Write(mapSerialId);
        writer.Write(new byte[56]);

        return writer.ToBytes();
    }

    public static ChannelSelectMyRoomResponse FromBytes(ReadOnlySpan<byte> data) => throw new NotImplementedException();
}
