using aisp.Common.Game;
using aisp.Common.Game.ServerScripts;
using aisp.Network;
using aisp.Network.Packets.Area;
using Microsoft.Extensions.Logging;

namespace aisp.Common.Handlers.Area;

public sealed class AreaQuestWorkGetHandler(IQuestService quests)
    : PacketHandlerBase<QuestWorkGetRequest, QuestGetWorkResponse>,
        IRequiresAuthenticatedSession
{
    public override PacketType RequestType => PacketType.QuestWorkGetRequest;
    public override PacketType ResponseType => PacketType.QuestGetWorkResponse;
    public override ServerType ServerType => ServerType.Area;

    public override async Task<QuestGetWorkResponse?> HandleAsync(
        QuestWorkGetRequest request,
        IPlayerSession session,
        CancellationToken ct = default
    ) => await quests.GetWorkResponseAsync(session, ct);
}

public sealed class AreaQuestHistoryGetHandler(IQuestService quests)
    : PacketHandlerBase<QuestHistoryGetRequest, QuestGetHistoryResponse>,
        IRequiresAuthenticatedSession
{
    public override PacketType RequestType => PacketType.QuestHistoryGetRequest;
    public override PacketType ResponseType => PacketType.QuestGetHistoryResponse;
    public override ServerType ServerType => ServerType.Area;

    public override async Task<QuestGetHistoryResponse?> HandleAsync(
        QuestHistoryGetRequest request,
        IPlayerSession session,
        CancellationToken ct = default
    ) => await quests.GetHistoryResponseAsync(session, ct);
}

public sealed class AreaEventQuestSelectExecRHandler(
    ServerScriptDispatcher serverScriptDispatcher,
    ILogger<AreaEventQuestSelectExecRHandler> logger
) : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.EventQuestSelectExecRRequest;
    public PacketType ResponseType => (PacketType)0;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        if (await serverScriptDispatcher.TryHandlePacketAsync(RequestType, payload, session, ct))
            return;

        logger.LogDebug(
            "Ignoring EventQuestSelectExecR from character {CharacterId}: no server script handled the packet",
            session.CharacterId
        );
    }
}
