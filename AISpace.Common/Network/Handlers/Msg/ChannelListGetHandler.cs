using AISpace.Common.Game;
using AISpace.Common.Network.Packets.Msg;
using Microsoft.Extensions.Logging;

namespace AISpace.Common.Network.Handlers.Msg;

public class ChannelListGetHandler(ILogger<ChannelListGetHandler> logger) : IPacketHandler
{
    public PacketType RequestType => PacketType.ChannelListGetRequest;
    public PacketType ResponseType => PacketType.ChannelListGetResponse;
    public MessageDomain Domain => MessageDomain.Msg;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, ClientConnection connection, CancellationToken ct = default)
    {
        // ХАРДКОД
        string myIp = "192.168.31.158"; 
        ushort areaPort = 50054; // Порт Area сервера

        logger.LogInformation($"[STEP 2] Msg -> Area (List). Sending IP: {myIp}:{areaPort}");

        var serverInfo = new ServerInfo(myIp, areaPort);
        var channels = new List<ChannelInfo> { new ChannelInfo(0, 0, 1, serverInfo) };
        var response = new ChannelListGetResponse(0, channels);
        await connection.SendAsync(ResponseType, response.ToBytes(), ct);
    }
}