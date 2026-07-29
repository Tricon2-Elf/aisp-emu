using AISpace.Network;
using AISpace.Network.Packets.Area;

namespace AISpace.Common.Game.ServerScripts;

public sealed class IntroductionChineseCosplayerServerScript(
    ServerScriptSession serverScriptSession
) : IServerScript
{
    private const string AwaitingDialogueSyncStep = "AwaitingDialogueSync";

    public string EventKey => ServerEvents.Keys.IntroductionChineseCosplayer;

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
            new EventMessageNotify(npcObjectId, context.Npc.Name, "Hello").ToBytes(),
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
        await serverScriptSession.CompleteAsync(
            session,
            request.Result,
            markComplete: request.Result == 0,
            ct
        );
        return true;
    }
}
