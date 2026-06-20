namespace AISpace.Network.Data;

public class ChannelInfo(uint ChannelId, uint CurrentUserCount, uint MaxUserCount, ServerInfo ServerInfo)
{

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(ChannelId);
        writer.Write((float)CurrentUserCount);
        writer.Write(MaxUserCount);
        writer.Write(ServerInfo.ToBytes());
        return writer.ToBytes();
    }
}
