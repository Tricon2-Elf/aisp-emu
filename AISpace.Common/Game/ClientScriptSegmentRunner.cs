using AISpace.Common.DAL.Entities;
using AISpace.Common.Game.ServerScripts;
using AISpace.Network;
using AISpace.Network.Packets.Area;

namespace AISpace.Common.Game;

public enum ClientScriptSegmentStatus : byte
{
    NotHandled = 0,
    InProgress = 1,
    Completed = 2,
    Failed = 3,
}

public readonly record struct ClientScriptSegmentResult(
    ClientScriptSegmentStatus Status,
    uint Result = 0
);

public sealed class ClientScriptSegmentRunner
{
    private const string ScriptPlayStep = "ClientScriptSegment.ScriptPlay";
    private const string FadeInStep = "ClientScriptSegment.FadeIn";
    private const string ScriptKeyDataKey = "clientScriptSegment.scriptKey";

    public async Task BeginAsync(
        IPlayerSession session,
        string clientScriptKey,
        CancellationToken ct = default
    )
    {
        if (
            session.ActiveEventKind != NpcEventKind.ServerScript
            || session.ServerScriptState is null
        )
            throw new InvalidOperationException(
                "Client script segments require an active server script."
            );

        session.ServerScriptState.Step = ScriptPlayStep;
        session.ServerScriptState.Data[ScriptKeyDataKey] = clientScriptKey;
        await session.SendAsync(
            PacketType.EventScriptPlayNotify,
            new EventScriptPlayNotify(ScriptedEvents.GetScriptLabel(clientScriptKey)).ToBytes(),
            ct
        );
    }

    public async Task<ClientScriptSegmentResult> TryHandleAsync(
        PacketType packetType,
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var state = session.ServerScriptState;
        if (session.ActiveEventKind != NpcEventKind.ServerScript || state is null)
            return new ClientScriptSegmentResult(ClientScriptSegmentStatus.NotHandled);

        if (packetType == PacketType.EventScriptPlayRequest && state.Step == ScriptPlayStep)
        {
            var request = EventScriptPlayRequest.FromBytes(payload.Span);
            if (request.Result != 0)
                return new ClientScriptSegmentResult(
                    ClientScriptSegmentStatus.Failed,
                    request.Result
                );

            state.Step = FadeInStep;
            await session.SendAsync(
                PacketType.EventFadeInNotify,
                new EventFadeNotify(1f, 255, 255, 255).ToBytes(),
                ct
            );
            return new ClientScriptSegmentResult(ClientScriptSegmentStatus.InProgress);
        }

        if (packetType == PacketType.EventFadeInRequest && state.Step == FadeInStep)
        {
            state.Data.Remove(ScriptKeyDataKey);
            return new ClientScriptSegmentResult(ClientScriptSegmentStatus.Completed);
        }

        return new ClientScriptSegmentResult(ClientScriptSegmentStatus.NotHandled);
    }
}
