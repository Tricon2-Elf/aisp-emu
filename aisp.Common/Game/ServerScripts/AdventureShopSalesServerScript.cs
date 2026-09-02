using aisp.Common.Config;
using aisp.Common.DAL.Entities;
using aisp.Common.DAL.Repositories;
using aisp.Common.Localisation;
using aisp.Network;
using aisp.Network.Packets.Area;
using Microsoft.Extensions.Options;

namespace aisp.Common.Game.ServerScripts;

/// <summary>
/// Drama disc shop 売上担当 clerk. Pays out the settled sales balance in デレ (the in-game currency) on the spot; otherwise says how much
/// is still waiting for the weekly settlement, or that nothing has sold yet.
/// It has to run as an event: the client only renders recv_event_message between recv_event_start and
/// recv_event_end, so a bare message from the NPC access handler is dropped.
/// </summary>
public sealed class AdventureShopSalesServerScript(
    ServerScriptSession serverScriptSession,
    ITextLocaliser localiser,
    IAdventureShopRepository shop,
    IOptions<ServerOptions> serverOptions
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

        var userId = session.User?.Id ?? session.UserId;
        var balances = await shop.GetBalancesAsync(userId, ct);
        string text;
        if (balances is { Collectable: > 0 })
        {
            var paid = await shop.PayoutAsync(userId, ct);
            if (paid is { Paid: > 0 })
            {
                if (session.User is not null)
                    session.User.AiPoints = paid.Value.AiPoints;
                await session.SendAsync(
                    PacketType.MoneyUpdatedAipoint,
                    new MoneyUpdatedAipointNotify(
                        (ulong)Math.Max(0, paid.Value.AiPoints)
                    ).ToBytes(),
                    ct
                );
                text = localiser.Get(session, L.Adventure.ShopSalesPaid, paid.Value.Paid);
            }
            else
            {
                text = localiser.Get(session, L.Adventure.ShopSalesEmpty);
            }
        }
        else if (balances is { Pending: > 0 })
        {
            var settlement = serverOptions.Value.AdventureSettlement;
            var next = TimeZoneInfo.ConvertTimeFromUtc(
                settlement.GetNextCutoffUtc(DateTime.UtcNow),
                settlement.ResolveTimeZone()
            );
            text = localiser.Get(
                session,
                L.Adventure.ShopSalesPending,
                balances.Pending,
                next.ToString("M/d HH:mm")
            );
        }
        else
        {
            text = localiser.Get(session, L.Adventure.ShopSalesEmpty);
        }

        var npcObjectId = checked((uint)context.Npc.NpcObjectId);
        await session.SendAsync(
            PacketType.EventMessageNotify,
            new EventMessageNotify(
                npcObjectId,
                localiser.Get(session, L.Npc.Name(context.Npc.NpcObjectId)),
                text
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
