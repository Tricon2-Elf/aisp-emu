using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Packets.Msg;
using Microsoft.Extensions.Logging;

namespace aisp.Common.Handlers.Msg;

public class CircleChatPostHandler(
    ILogger<CircleChatPostHandler> logger,
    ICircleRepository circles,
    SharedState state
) : IPacketHandler, IRequiresAuthenticatedSession
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
        if (!state.TryGetCircleChat(session.ConnectionId, out var circleId))
        {
            await session.SendAsync(
                ResponseType,
                new CircleChatPostResponse(req.MessageId, (uint)CircleResult.Failed).ToBytes(),
                ct
            );
            return;
        }

        var membership = await circles.GetMembershipAsync(circleId, (int)session.CharacterId, ct);
        if (membership is null)
        {
            await session.SendAsync(
                ResponseType,
                new CircleChatPostResponse(req.MessageId, (uint)CircleResult.NotMember).ToBytes(),
                ct
            );
            return;
        }

        logger.LogInformation(
            "[CIRCLE CHAT] From:{CharacterId} Circle:{CircleId}: {Message}",
            session.CharacterId,
            circleId,
            req.Message
        );

        await session.SendAsync(
            ResponseType,
            new CircleChatPostResponse(req.MessageId, 0).ToBytes(),
            ct
        );

        var forward = new CircleChatForwardNotify(session.CharacterId, req.Message).ToBytes();
        foreach (var client in state.GetCircleChatClients(circleId))
        {
            if (client.ConnectionId != session.ConnectionId)
                await client.SendAsync(PacketType.CircleChatForwardNotify, forward, ct);
        }
    }
}
