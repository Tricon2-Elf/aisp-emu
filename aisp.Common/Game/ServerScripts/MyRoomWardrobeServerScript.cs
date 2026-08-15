using aisp.Common.Localisation;
using aisp.Network;
using aisp.Network.Data;
using aisp.Network.Packets.Area;

namespace aisp.Common.Game.ServerScripts;

public sealed class MyRoomWardrobeServerScript(
    ServerScriptSession serverScriptSession,
    ITextLocaliser localiser
) : IServerScript
{
    private const string AwaitingSelectionStep = "AwaitingSelection";

    public string EventKey => ServerEvents.Keys.MyRoomWardrobe;
    public EventCompletionPolicy CompletionPolicy => EventCompletionPolicy.Replayable;

    public async Task StartAsync(
        IPlayerSession session,
        ServerScriptContext context,
        CancellationToken ct = default
    )
    {
        session.ServerScriptState!.Step = AwaitingSelectionStep;
        await session.SendAsync(
            PacketType.EventSelectInitNotify,
            new EventSelectInitNotify { SelectType = EventSelectType.Popup }.ToBytes(),
            ct
        );
        await session.SendAsync(
            PacketType.EventSelectPushNotify,
            new EventSelectPushNotify
            {
                SelectName = localiser.Get(session, L.Script.MyRoom.WardrobeUse),
            }.ToBytes(),
            ct
        );
        await session.SendAsync(
            PacketType.EventSelectPushNotify,
            new EventSelectPushNotify
            {
                SelectName = localiser.Get(session, L.Script.MyRoom.WardrobeSkip),
            }.ToBytes(),
            ct
        );
        await session.SendAsync(
            PacketType.EventSelectExecNotify,
            new EventSelectExecNotify
            {
                Text = localiser.Get(session, L.Script.MyRoom.WardrobePrompt),
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
        if (
            state is null
            || !string.Equals(state.EventKey, EventKey, StringComparison.Ordinal)
            || state.Step != AwaitingSelectionStep
            || packetType != PacketType.EventSelectExecRRequest
        )
            return false;

        var request = EventSelectExecRRequest.FromBytes(payload.Span);
        if (request.Result != 0 || request.SelectNo == 1)
        {
            await serverScriptSession.CompleteAsync(
                session,
                request.Result,
                markComplete: false,
                ct
            );
            return true;
        }

        if (request.SelectNo != 0)
        {
            await serverScriptSession.AbortAsync(session, 1, ct);
            return true;
        }

        await serverScriptSession.CompleteAsync(session, 0, markComplete: false, ct);
        await StorageSession.OpenAsync(session, StorageOpenContext.Wardrobe, ct);
        return true;
    }
}
