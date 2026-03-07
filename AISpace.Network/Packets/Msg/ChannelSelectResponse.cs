using AISpace.Network.Data;

namespace AISpace.Network.Packets.Msg;

public class ChannelSelectResponse(uint result, ServerInfo serverInfo, uint mapId, uint mapSerialId) : IPacket<ChannelSelectResponse>
{
    public uint Result = result;
    public ServerInfo ServerInfo = serverInfo;
    public uint MapID = mapId;
    public uint MapSerialID = mapSerialId;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(Result);
        writer.Write(ServerInfo.ToBytes());
        writer.Write(MapID);
        writer.Write(MapSerialID);
        return writer.ToBytes();
    }

    public static ChannelSelectResponse FromBytes(ReadOnlySpan<byte> data) => throw new NotImplementedException();
}
