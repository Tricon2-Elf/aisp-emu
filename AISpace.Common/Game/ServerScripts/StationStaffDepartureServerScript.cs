using AISpace.Common.DAL.Repositories;
using AISpace.Network;
using AISpace.Network.Packets.Area;
using Microsoft.Extensions.Logging;

namespace AISpace.Common.Game.ServerScripts;

public sealed class StationStaffDepartureServerScript(
    ICharacterRepository characterRepository,
    ClientScriptSegmentRunner clientScriptSegmentRunner,
    ServerScriptSession serverScriptSession,
    DirectMapLinkTransitionService directMapLinkTransitionService,
    ILogger<StationStaffDepartureServerScript> logger
) : IServerScript
{
    private const uint EventFailure = 1;
    private const string AwaitingRegistrationMessageSyncStep = "AwaitingRegistrationMessageSync";
    private const string DestinationMapDataKey = "destinationMapId";

    private static readonly IReadOnlyDictionary<uint, DepartureRoute> Routes = new Dictionary<
        uint,
        DepartureRoute
    >
    {
        [1] = new(ScriptedEvents.Keys.IntroductionMyRoomDaCapo, 10010200),
        [2] = new(ScriptedEvents.Keys.IntroductionMyRoomClannad, 10020200),
        [3] = new(ScriptedEvents.Keys.IntroductionMyRoomShuffle, 10030200),
    };

    public string EventKey => ServerEvents.Keys.StationStaffDeparture;

    public async Task StartAsync(
        IPlayerSession session,
        ServerScriptContext context,
        CancellationToken ct = default
    )
    {
        var character = await characterRepository.GetByIdAsync((int)session.CharacterId, ct);
        if (character is null)
        {
            logger.LogWarning(
                "Aborting server script {EventKey} for character {CharacterId}: character not found",
                EventKey,
                session.CharacterId
            );
            await serverScriptSession.AbortAsync(session, EventFailure, ct);
            return;
        }

        session.Character = character;
        if (character.HomeIslandId == 0)
        {
            session.ServerScriptState!.Step = AwaitingRegistrationMessageSyncStep;
            var npcObjectId = checked((uint)context.Npc.NpcObjectId);
            await session.SendAsync(
                PacketType.EventMessageNotify,
                new EventMessageNotify(
                    npcObjectId,
                    context.Npc.Name,
                    "Please register at the Sotokanda Building first."
                ).ToBytes(),
                ct
            );
            await session.SendAsync(
                PacketType.EventMessageCloseNotify,
                new EventMessageCloseNotify().ToBytes(),
                ct
            );
            await session.SendAsync(
                PacketType.EventSyncNotify,
                new EventSyncNotify().ToBytes(),
                ct
            );
            return;
        }

        if (!Routes.TryGetValue(character.HomeIslandId, out var route))
        {
            logger.LogWarning(
                "Aborting server script {EventKey} for character {CharacterId}: unsupported home island {IslandId}",
                EventKey,
                session.CharacterId,
                character.HomeIslandId
            );
            await serverScriptSession.AbortAsync(session, EventFailure, ct);
            return;
        }

        session.ServerScriptState!.Data[DestinationMapDataKey] = route.DestinationMapId;
        logger.LogInformation(
            "Starting client script segment {ClientScriptKey} for character {CharacterId} before travel to map {DestinationMapId}",
            route.ClientScriptKey,
            session.CharacterId,
            route.DestinationMapId
        );
        await clientScriptSegmentRunner.BeginAsync(session, route.ClientScriptKey, ct);
    }

    public async Task<bool> TryHandlePacketAsync(
        PacketType packetType,
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var state = session.ServerScriptState;
        if (state is null || !string.Equals(state.EventKey, EventKey, StringComparison.Ordinal))
            return false;

        if (
            state.Step == AwaitingRegistrationMessageSyncStep
            && packetType == PacketType.EventSyncRRequest
        )
        {
            var request = EventSyncRRequest.FromBytes(payload.Span);
            await serverScriptSession.CompleteAsync(
                session,
                request.Result,
                markComplete: false,
                ct
            );
            return true;
        }

        var segmentResult = await clientScriptSegmentRunner.TryHandleAsync(
            packetType,
            payload,
            session,
            ct
        );
        switch (segmentResult.Status)
        {
            case ClientScriptSegmentStatus.NotHandled:
                return false;
            case ClientScriptSegmentStatus.InProgress:
                return true;
            case ClientScriptSegmentStatus.Failed:
                logger.LogWarning(
                    "Aborting server script {EventKey} for character {CharacterId}: client script result {Result}",
                    EventKey,
                    session.CharacterId,
                    segmentResult.Result
                );
                await serverScriptSession.AbortAsync(
                    session,
                    segmentResult.Result == 0 ? EventFailure : segmentResult.Result,
                    ct
                );
                return true;
            case ClientScriptSegmentStatus.Completed:
                var destinationMapId = (uint)state.Data[DestinationMapDataKey];
                await serverScriptSession.CompleteAsync(session, 0, markComplete: false, ct);
                if (
                    !await directMapLinkTransitionService.TryTeleportToMapAsync(
                        session,
                        destinationMapId,
                        ct
                    )
                )
                    logger.LogWarning(
                        "Server script {EventKey} completed for character {CharacterId}, but teleport to map {DestinationMapId} failed",
                        EventKey,
                        session.CharacterId,
                        destinationMapId
                    );
                return true;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private sealed record DepartureRoute(string ClientScriptKey, uint DestinationMapId);
}
