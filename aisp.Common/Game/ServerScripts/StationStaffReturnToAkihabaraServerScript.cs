using aisp.Common.DAL.Entities;
using aisp.Network;
using aisp.Network.Packets.Area;
using Microsoft.Extensions.Logging;

namespace aisp.Common.Game.ServerScripts;

public sealed class StationStaffReturnToAkihabaraServerScript(
    ServerScriptSession serverScriptSession,
    DirectMapLinkTransitionService directMapLinkTransitionService,
    ILogger<StationStaffReturnToAkihabaraServerScript> logger
) : IServerScript
{
    public const uint AkihabaraMapId = 10_990_100;
    private const string AwaitingMessageSyncStep = "AwaitingMessageSync";

    public string EventKey => ServerEvents.Keys.StationStaffReturnToAkihabara;
    public EventCompletionPolicy CompletionPolicy => EventCompletionPolicy.Replayable;

    public async Task StartAsync(
        IPlayerSession session,
        ServerScriptContext context,
        CancellationToken ct = default
    )
    {
        session.ServerScriptState!.Step = AwaitingMessageSyncStep;

        var npcObjectId = checked((uint)context.Npc.NpcObjectId);
        await session.SendAsync(
            PacketType.EventMessageNotify,
            new EventMessageNotify(
                npcObjectId,
                context.Npc.Name,
                "I'll take you to Akihabara"
            ).ToBytes(),
            ct
        );
        await session.SendAsync(
            PacketType.EventMessageCloseNotify,
            new EventMessageCloseNotify().ToBytes(),
            ct
        );
        await session.SendAsync(PacketType.EventSyncNotify, new EventSyncNotify().ToBytes(), ct);
    }

    public async Task<bool> TryHandlePacketAsync(
        PacketType packetType,
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var state = session.ServerScriptState;
        if (
            state is null
            || !string.Equals(state.EventKey, EventKey, StringComparison.Ordinal)
            || state.Step != AwaitingMessageSyncStep
            || packetType != PacketType.EventSyncRRequest
        )
            return false;

        var request = EventSyncRRequest.FromBytes(payload.Span);
        if (request.Result != 0)
        {
            await serverScriptSession.AbortAsync(session, request.Result, ct);
            return true;
        }

        await serverScriptSession.CompleteAsync(session, 0, markComplete: false, ct);
        if (
            !await directMapLinkTransitionService.TryTeleportToMapAsync(session, AkihabaraMapId, ct)
        )
            logger.LogWarning(
                "Server script {EventKey} completed for character {CharacterId}, but teleport to map {DestinationMapId} failed",
                EventKey,
                session.CharacterId,
                AkihabaraMapId
            );

        return true;
    }
}
