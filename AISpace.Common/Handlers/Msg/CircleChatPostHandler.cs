using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Packets.Msg;
using Microsoft.Extensions.Logging;

namespace AISpace.Common.Handlers.Msg;

public class CircleChatPostHandler(ILogger<CircleChatPostHandler> logger, SharedState state)
    : IPacketHandler,
        IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.CircleChatPostRequest;
    public PacketType ResponseType => PacketType.CircleChatPostResponse;
    public ServerType ServerType => ServerType.Msg;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var req = CircleChatPostRequest.FromBytes(payload.Span);

        logger.LogInformation(
            $"[CIRCLE CHAT] From:{session.CharacterId} Circle:{req.CircleId}: {req.Message}"
        );

        var response = new CmdExecResponse(0, 0);
        await session.SendAsync(ResponseType, response.ToBytes(), ct);

        var forwardData = new CircleChatForwardNotify(
            req.CircleId,
            session.CharacterId,
            req.Message,
            req.BalloonId
        ).ToBytes();

        foreach (var client in state.GetServerClients(ServerType.Msg))
        {
            if (client.IsAuthenticated && client.ConnectionId != session.ConnectionId)
            {
                await client.SendAsync(PacketType.CircleChatForwardNotify, forwardData, ct);
            }
        }
    }
}
