using aisp.Common.Game;
using aisp.Network;
using Microsoft.Extensions.Logging;

namespace aisp.Common.Handlers.Area;

public class AreaMissionResultCloseHandler(ILogger<AreaMissionResultCloseHandler> logger)
    : IPacketHandler,
        IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.MissionResultCloseRequest;
    public PacketType ResponseType => (PacketType)0;
    public ServerType ServerType => ServerType.Area;

    public Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        logger.LogInformation(
            "Mission result closed for character {CharacterId}",
            session.CharacterId
        );
        return Task.CompletedTask;
    }
}

public class AreaMissionOutmapChoiceOpenRHandler(
    ILogger<AreaMissionOutmapChoiceOpenRHandler> logger
) : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.MissionOutmapChoiceOpenRRequest;
    public PacketType ResponseType => (PacketType)0;
    public ServerType ServerType => ServerType.Area;

    public Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        logger.LogInformation(
            "Mission out-map choice from character {CharacterId}",
            session.CharacterId
        );
        return Task.CompletedTask;
    }
}
