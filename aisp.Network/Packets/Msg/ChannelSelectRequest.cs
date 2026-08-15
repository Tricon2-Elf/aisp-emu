using aisp.Network;

namespace aisp.Network.Packets.Msg;

public class ChannelSelectRequest : IIncomingPacket<ChannelSelectRequest>
{
    public uint ChannelID;

    public static ChannelSelectRequest FromBytes(ReadOnlySpan<byte> data)
    {
        ChannelSelectRequest req = new();
        PacketReader reader = new(data);
        var channelID = reader.ReadUInt();
        req.ChannelID = channelID;
        return req;
    }
}
