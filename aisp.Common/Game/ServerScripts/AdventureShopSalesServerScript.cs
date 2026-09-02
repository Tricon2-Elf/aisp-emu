using aisp.Common.DAL.Entities;
using aisp.Common.Localisation;
using aisp.Network;
using aisp.Network.Packets.Area;

namespace aisp.Common.Game.ServerScripts;

/// <summary>
/// Drama disc shop 売上担当 clerk. Sales payouts are not modelled, so this is a single dialogue line.
/// It has to run as an event: the client only renders recv_event_message between recv_event_start and
/// recv_event_end, so a bare message from the NPC access handler is dropped.
/// </summary>
public sealed class AdventureShopSalesServerScript(
    ServerScriptSession serverScriptSession,
    ITextLocaliser localiser
) : IServerScript
{
    private const string AwaitingDialogueSyncStep = "AwaitingDialogueSync";

    public string EventKey => ServerEvents.Keys.AdventureShopSales;

    public EventCompletionPolicy CompletionPolicy => EventCompletionPolicy.Replayable;

    public async Task StartAsync(
        IPlayerSession session,
        ServerScriptContext context,
        CancellationToken ct = default
    )
    {
        session.ServerScriptState!.Step = AwaitingDialogueSyncStep;

        var npcObjectId = checked((uint)context.Npc.NpcObjectId);
        await session.SendAsync(
            PacketType.EventMessageNotify,
            new EventMessageNotify(
                npcObjectId,
                localiser.Get(session, L.Npc.Name(context.Npc.NpcObjectId)),
                localiser.Get(session, L.Adventure.ShopSalesEmpty)
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
            || state.Step != AwaitingDialogueSyncStep
            || packetType != PacketType.EventSyncRRequest
        )
            return false;

        var request = EventSyncRRequest.FromBytes(payload.Span);
        await serverScriptSession.CompleteAsync(session, request.Result, markComplete: false, ct);
        return true;
    }
}
