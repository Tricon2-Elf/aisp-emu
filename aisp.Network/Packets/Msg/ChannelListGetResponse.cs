using aisp.Network.Data;

namespace aisp.Network.Packets.Msg;

public class ChannelListGetResponse(uint result, List<ChannelInfo> channels) : IOutgoingPacket
{
    public uint Result = result;
    public List<ChannelInfo> Channels = channels;

    public byte[] ToBytes()
    {
        var writer = new PacketWriter();
        writer.Write(Result);
        writer.Write((uint)Channels.Count);
        foreach (var channel in Channels)
            writer.Write(channel.ToBytes());
        return writer.ToBytes();
    }
}
