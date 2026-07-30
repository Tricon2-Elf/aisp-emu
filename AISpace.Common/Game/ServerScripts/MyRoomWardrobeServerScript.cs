using AISpace.Network;
using AISpace.Network.Data;
using AISpace.Network.Packets.Area;

namespace AISpace.Common.Game.ServerScripts;

public sealed class MyRoomWardrobeServerScript(ServerScriptSession serverScriptSession)
    : IServerScript
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
            new EventSelectPushNotify { SelectName = "倉庫を利用する" }.ToBytes(),
            ct
        );
        await session.SendAsync(
            PacketType.EventSelectPushNotify,
            new EventSelectPushNotify { SelectName = "使用しない" }.ToBytes(),
            ct
        );
        await session.SendAsync(
            PacketType.EventSelectExecNotify,
            new EventSelectExecNotify { Text = "倉庫を利用しますか？" }.ToBytes(),
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

        var deposit = (ulong)Math.Max(0, session.User?.StorageDeposit ?? 0);
        await serverScriptSession.CompleteAsync(session, 0, markComplete: false, ct);
        await session.SendAsync(
            PacketType.StorageOpenedNotify,
            new StorageOpenedNotify(deposit).ToBytes(),
            ct
        );
        return true;
    }
}
