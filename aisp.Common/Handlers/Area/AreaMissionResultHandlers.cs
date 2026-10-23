using aisp.Common.Game;
using aisp.Network;
using Microsoft.Extensions.Logging;

namespace aisp.Common.Handlers.Area;

public class AreaMissionResultCloseHandler(
    ILogger<AreaMissionResultCloseHandler> logger,
    MissionRegistry missions
) : IPacketHandler, IRequiresAuthenticatedSession
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
        missions.RemoveMember(session);
        return Task.CompletedTask;
    }
}

public class AreaMissionOutmapChoiceOpenRHandler(
    ILogger<AreaMissionOutmapChoiceOpenRHandler> logger,
    MissionRegistry missions
) : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.MissionOutmapChoiceOpenRRequest;
    public PacketType ResponseType => (PacketType)0;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var result = 0u;
        if (payload.Length >= 4)
            result = new PacketReader(payload.Span).ReadUInt();

        logger.LogInformation(
            "Mission out-map choice {Result} from character {CharacterId}",
            result,
            session.CharacterId
        );

        // 0 confirms leaving. That ends the mission as a fail.
        if (result == 0 && missions.TryGet(session) is { } mission)
        {
            await TpsMissionGameOver.FailAsync(mission, logger, ct);
            missions.RemoveMember(session);
        }
    }
}
