namespace AISpace.Network.Data;

public class ChannelInfo(uint channelID, uint currentUserCount, uint maxUserCount, ServerInfo serverInfo)
{
    public uint channelID = channelID;
    public uint currentUserCount = currentUserCount;
    public uint maxUserCount = maxUserCount;
    public ServerInfo serverInfo = serverInfo;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(channelID);
        writer.Write(currentUserCount);
        writer.Write(maxUserCount);
        writer.Write(serverInfo.ToBytes());
        return writer.ToBytes();
    }
}
