using AISpace.Network.Data;

namespace AISpace.Network.Packets.Msg;

public class ChannelSelectMyRoomResponse(
    uint result,
    ServerInfo serverInfo,
    uint mapId,
    uint mapSerialId,
    MyRoomData room
) : IOutgoingPacket
{
    public uint Result = result;
    public ServerInfo ServerInfo = serverInfo;
    public uint MapID = mapId;
    public uint MapSerialID = mapSerialId;
    public MyRoomData Room = room;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();

        writer.Write(Result);
        writer.Write(ServerInfo.ToBytes());
        writer.Write(MapID);
        writer.Write(MapSerialID);
        writer.Write(Room.ToBytes());

        return writer.ToBytes();
    }
}
