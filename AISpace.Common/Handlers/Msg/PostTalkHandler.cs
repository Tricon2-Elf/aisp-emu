using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Packets.Msg;

namespace AISpace.Common.Handlers.Msg;

public class PostTalkHandler(SharedState state) : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.PostTalkRequest;
    public PacketType ResponseType => PacketType.PostTalkResponse;
    public ServerType ServerType => ServerType.Msg;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var chatRequest = PostTalkRequest.FromBytes(payload.Span);

        var response = new PostTalkResponse(chatRequest.MessageID, 0);
        await session.SendAsync(ResponseType, response.ToBytes(), ct);

        var fromId = session.CharacterId;
        if (fromId == 0)
        {
            var areaSession = state.GetAreaSessionByUserId(session.UserId);
            fromId = areaSession?.CharacterId ?? 0;
        }

        var forwardPacket = new TalkForwardNotify(
            fromId,
            chatRequest.DistID,
            chatRequest.Message,
            chatRequest.BalloonID
        );
        byte[] broadcastData = forwardPacket.ToBytes();

        foreach (var client in state.GetServerClients(ServerType.Msg))
        {
            if (client.IsAuthenticated && client.ConnectionId != session.ConnectionId)
            {
                await client.SendAsync(PacketType.TalkForwardNotify, broadcastData, ct);
            }
        }
    }
}
