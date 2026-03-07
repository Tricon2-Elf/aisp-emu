using AISpace.Network;
using AISpace.Network.Packets.Msg;

namespace AISpace.Common.Handlers.Msg;

public class ChannelSelectHandler(ILogger<ChannelSelectHandler> logger, IServiceScopeFactory scopeFactory, IOptions<ServerOptions> serverOptions, IChannelRepository channelRepo) : IPacketHandler
{
    public PacketType RequestType => PacketType.ChannelSelectRequest;
    public PacketType ResponseType => PacketType.ChannelSelectResponse;
    public MessageDomain Domain => MessageDomain.Msg;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, ClientConnection connection, CancellationToken ct = default)
    {
        var request = ChannelSelectRequest.FromBytes(payload.Span);
        logger.LogInformation("ChannelSelectRequest from user {UserId}: ChannelID {ChannelId}", connection.User?.Id ?? 0, request.ChannelID);

        var channel = await channelRepo.GetByChannelNumAsync((int)request.ChannelID, ct);
        if (channel == null)
        {
            logger.LogWarning("Channel not found: ChannelNum {ChannelId}", request.ChannelID);
            var failResponse = new ChannelSelectResponse(1, new ServerInfo("0.0.0.0", 0), 0, 0);
            await connection.SendAsync(ResponseType, failResponse.ToBytes(), ct);
            return;
        }

        string resolvedIp = serverOptions.Value.ResolveAddress(channel.IP);
        var serverInfo = new ServerInfo(resolvedIp, channel.Port);
        uint mapId = channel.MapId;
        logger.LogInformation("ChannelSelect: sending Area server {ResolvedIp}:{Port} (MapId={MapId})", resolvedIp, channel.Port, mapId);

        using (var scope = scopeFactory.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MainContext>();
            if (connection.User != null)
            {
                var character = await db.Characters.FirstOrDefaultAsync(c => c.UserId == connection.User.Id, ct);
                if (character != null)
                {
                    character.CurrentMapId = mapId;
                    await db.SaveChangesAsync(ct);
                }
            }
        }

        var response = new ChannelSelectResponse(0, serverInfo, mapId, mapId);
        await connection.SendAsync(ResponseType, response.ToBytes(), ct);
    }
}
