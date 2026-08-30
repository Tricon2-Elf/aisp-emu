using aisp.Common.DAL.Repositories;
using aisp.Common.Localisation;
using aisp.Network;
using aisp.Network.Data;
using aisp.Network.Packets.Area;
using Microsoft.Extensions.Logging;

namespace aisp.Common.Game.ServerScripts;

public sealed class StationStaffDepartureServerScript(
    ICharacterRepository characterRepository,
    ServerScriptSession serverScriptSession,
    DirectMapLinkTransitionService directMapLinkTransitionService,
    ITextLocaliser localiser,
    ILogger<StationStaffDepartureServerScript> logger
) : IServerScript
{
    public const uint DaCapoShoppingStreetMapId = 10_010_200;
    public const uint ShuffleShoppingStreetMapId = 10_030_200;
    public const uint ClannadShoppingStreetMapId = 10_020_200;

    private const uint EventFailure = 1;
    private const string AwaitingRegistrationMessageSyncStep = "AwaitingRegistrationMessageSync";
    private const string AwaitingIslandSelectStep = "AwaitingIslandSelect";

    private static readonly (uint DestinationMapId, LocKey Label)[] Destinations =
    [
        (DaCapoShoppingStreetMapId, L.Island.Name(1)),
        (ShuffleShoppingStreetMapId, L.Island.Name(3)),
        (ClannadShoppingStreetMapId, L.Island.Name(2)),
    ];

    public string EventKey => ServerEvents.Keys.StationStaffDeparture;
    public EventCompletionPolicy CompletionPolicy => EventCompletionPolicy.Replayable;

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
                    localiser.Get(session, L.Npc.Name(context.Npc.NpcObjectId)),
                    localiser.Get(session, L.Script.StationStaff.RegisterFirst)
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

        session.ServerScriptState!.Step = AwaitingIslandSelectStep;
        await session.SendAsync(
            PacketType.EventSelectInitNotify,
            new EventSelectInitNotify { SelectType = EventSelectType.Dialogue }.ToBytes(),
            ct
        );
        foreach (var (_, label) in Destinations)
            await session.SendAsync(
                PacketType.EventSelectPushNotify,
                new EventSelectPushNotify { SelectName = localiser.Get(session, label) }.ToBytes(),
                ct
            );
        await session.SendAsync(
            PacketType.EventSelectExecNotify,
            new EventSelectExecNotify
            {
                Text = localiser.Get(session, L.Script.StationStaff.ChooseIsland),
            }.ToBytes(),
            ct
        );
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
            var syncRequest = EventSyncRRequest.FromBytes(payload.Span);
            await serverScriptSession.CompleteAsync(
                session,
                syncRequest.Result,
                markComplete: false,
                ct
            );
            return true;
        }

        if (
            state.Step != AwaitingIslandSelectStep
            || packetType != PacketType.EventSelectExecRRequest
        )
            return false;

        var request = EventSelectExecRRequest.FromBytes(payload.Span);
        if (request.Result != 0)
        {
            await serverScriptSession.CompleteAsync(
                session,
                request.Result,
                markComplete: false,
                ct
            );
            return true;
        }

        if (request.SelectNo >= Destinations.Length)
        {
            logger.LogWarning(
                "Rejecting server script {EventKey} for character {CharacterId}: invalid island selection {SelectNo}",
                EventKey,
                session.CharacterId,
                request.SelectNo
            );
            await serverScriptSession.AbortAsync(session, EventFailure, ct);
            return true;
        }

        var destinationMapId = Destinations[request.SelectNo].DestinationMapId;
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
    }
}
