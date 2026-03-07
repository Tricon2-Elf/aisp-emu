using AISpace.Network.Data;

namespace AISpace.Network.Packets.Msg;

public class ChannelListGetResponse(uint result, List<ChannelInfo> channels) : IPacket<ChannelListGetResponse>
{
    public uint Result = result;
    public List<ChannelInfo> Channels = channels;

    public static ChannelListGetResponse FromBytes(ReadOnlySpan<byte> data)
    {
        throw new NotImplementedException();
    }

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
