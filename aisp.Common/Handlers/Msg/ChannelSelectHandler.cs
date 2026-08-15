using aisp.Common.Config;
using aisp.Common.DAL;
using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Data;
using aisp.Network.Packets.Msg;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace aisp.Common.Handlers.Msg;

public class ChannelSelectHandler(
    ILogger<ChannelSelectHandler> logger,
    IServiceScopeFactory scopeFactory,
    IOptions<ServerOptions> serverOptions,
    IChannelRepository channelRepo
) : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.ChannelSelectRequest;
    public PacketType ResponseType => PacketType.ChannelSelectResponse;
    public ServerType ServerType => ServerType.Msg;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var request = ChannelSelectRequest.FromBytes(payload.Span);
        logger.LogInformation(
            "ChannelSelectRequest from user {UserId}: ChannelID {ChannelId}",
            session.User?.Id ?? 0,
            request.ChannelID
        );

        var channel = await channelRepo.GetByChannelNumAsync((int)request.ChannelID, ct);
        if (channel == null)
        {
            logger.LogWarning("Channel not found: ChannelNum {ChannelId}", request.ChannelID);
            var failResponse = new ChannelSelectResponse(1, new ServerInfo("0.0.0.0", 0), 0, 0);
            await session.SendAsync(ResponseType, failResponse.ToBytes(), ct);
            return;
        }

        string resolvedIp = serverOptions.Value.ResolveAddress(channel.IP);
        var serverInfo = new ServerInfo(resolvedIp, channel.Port);
        uint mapId = channel.MapId;
        logger.LogInformation(
            "ChannelSelect: sending Area server {ResolvedIp}:{Port} (MapId={MapId})",
            resolvedIp,
            channel.Port,
            mapId
        );

        session.ChannelId = channel.ChannelNum;
        session.MapId = mapId;

        using (var scope = scopeFactory.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MainContext>();
            if (session.User != null)
            {
                var character = await db.Characters.FirstOrDefaultAsync(
                    c => c.UserId == session.User.Id,
                    ct
                );
                if (character != null)
                {
                    character.CurrentMapId = mapId;
                    await db.SaveChangesAsync(ct);
                }
            }
        }

        var response = new ChannelSelectResponse(0, serverInfo, mapId, mapId);
        await session.SendAsync(ResponseType, response.ToBytes(), ct);
    }
}
