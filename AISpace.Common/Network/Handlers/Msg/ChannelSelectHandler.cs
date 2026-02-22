using AISpace.Common.Game;
using AISpace.Common.Network.Packets.Msg;
using Microsoft.Extensions.Logging;

namespace AISpace.Common.Network.Handlers.Msg;

public class ChannelSelectHandler(ILogger<ChannelSelectHandler> logger) : IPacketHandler
{
    public PacketType RequestType => PacketType.ChannelSelectRequest;
    public PacketType ResponseType => PacketType.ChannelSelectResponse;
    public MessageDomain Domain => MessageDomain.Msg;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, ClientConnection connection, CancellationToken ct = default)
    {
        string myIp = "192.168.31.158"; 

        uint mapID = 10030100; 

        logger.LogInformation($"[MAP] Sending player to Map ID: {mapID}");

        var response = new ChannelSelectResponse(0, new ServerInfo(myIp, 50054), mapID, mapID);
        await connection.SendAsync(ResponseType, response.ToBytes(), ct);
    }
}