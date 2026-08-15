using aisp.Common.Config;
using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Packets.Area;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace aisp.Common.Handlers.Area;

public sealed class AreaNicoliveReloadHandler(
    IOptions<ServerOptions> serverOptions,
    ILogger<AreaNicoliveReloadHandler> logger
) : PacketHandlerBase<NicoliveReloadRequest, NotifyNicoliveReload>, IRequiresAuthenticatedSession
{
    public override PacketType RequestType => PacketType.NicoliveReloadRequest;
    public override PacketType ResponseType => PacketType.NotifyNicoliveReload;
    public override ServerType ServerType => ServerType.Area;

    public override Task<NotifyNicoliveReload?> HandleAsync(
        NicoliveReloadRequest request,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var liveId = serverOptions.Value.NicoLive.LiveId?.Trim() ?? "";
        logger.LogInformation(
            "Nico Live billboard reload requested by character {CharacterId} on map {MapId}; returning live ID {LiveId}",
            session.CharacterId,
            session.MapId,
            string.IsNullOrEmpty(liveId) ? "(disabled)" : liveId
        );

        return Task.FromResult<NotifyNicoliveReload?>(new NotifyNicoliveReload(liveId));
    }
}
