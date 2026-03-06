using AISpace.Common.Config;
using AISpace.Common.Game;
using AISpace.Common.Network.Packets.Msg;
using Microsoft.Extensions.Options;

namespace AISpace.Common.Network.Handlers.Msg;

public class ChannelListGetHandler(IOptions<ServerOptions> serverOptions) : IPacketHandler
{
    public PacketType RequestType => PacketType.ChannelListGetRequest;
    public PacketType ResponseType => PacketType.ChannelListGetResponse;
    public MessageDomain Domain => MessageDomain.Msg;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, ClientConnection connection, CancellationToken ct = default)
    {
        string myIp = serverOptions.Value.ResolveAddress("localhost");
        ushort areaPort = 50054;

        var serverInfo = new ServerInfo(myIp, areaPort);
        var channels = new List<ChannelInfo> { new(1, 250, 1000, serverInfo), new(2, 0, 1000, serverInfo), new(3, 0, 1000, serverInfo), new(4, 0, 1000, serverInfo), new(5, 0, 1000, serverInfo), new(6, 0, 1000, serverInfo), new(7, 0, 1000, serverInfo), new(8, 0, 1000, serverInfo) };

        var response = new ChannelListGetResponse(0, channels);
        await connection.SendAsync(ResponseType, response.ToBytes(), ct);
    }
}
