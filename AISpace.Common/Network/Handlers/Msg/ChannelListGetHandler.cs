using AISpace.Common.Game;
using AISpace.Common.Network.Packets.Msg;

namespace AISpace.Common.Network.Handlers.Msg;

public class ChannelListGetHandler : IPacketHandler
{
    public PacketType RequestType => PacketType.ChannelListGetRequest;
    public PacketType ResponseType => PacketType.ChannelListGetResponse;
    public MessageDomain Domain => MessageDomain.Msg;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, ClientConnection connection, CancellationToken ct = default)
    {
        string myIp = "192.168.31.158"; 
        ushort areaPort = 50054;

        var serverInfo = new ServerInfo(myIp, areaPort);
        var channels = new List<ChannelInfo>();
        
        channels.Add(new ChannelInfo(1, 250, 1000, serverInfo));
        channels.Add(new ChannelInfo(2, 0, 1000, serverInfo));
        channels.Add(new ChannelInfo(3, 0, 1000, serverInfo));
        channels.Add(new ChannelInfo(4, 0, 1000, serverInfo));
        channels.Add(new ChannelInfo(5, 0, 1000, serverInfo));
        channels.Add(new ChannelInfo(6, 0, 1000, serverInfo));
        channels.Add(new ChannelInfo(7, 0, 1000, serverInfo));
        channels.Add(new ChannelInfo(8, 0, 1000, serverInfo));

        var response = new ChannelListGetResponse(0, channels);
        await connection.SendAsync(ResponseType, response.ToBytes(), ct);
    }
}
