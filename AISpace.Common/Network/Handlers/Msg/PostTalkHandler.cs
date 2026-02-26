using AISpace.Common.Game;
using AISpace.Common.Network.Packets;
using AISpace.Common.Network.Packets.Msg;

namespace AISpace.Common.Network.Handlers.Msg;

public class PostTalkHandler(SharedState state) : IPacketHandler
{
    public PacketType RequestType => PacketType.PostTalkRequest;
    public PacketType ResponseType => PacketType.PostTalkResponse;
    public MessageDomain Domain => MessageDomain.Msg;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, ClientConnection connection, CancellationToken ct = default)
    {
        var chatRequest = PostTalkRequest.FromBytes(payload.Span);

        var response = new PostTalkResponse(chatRequest.MessageID, 0);
        await connection.SendAsync(ResponseType, response.ToBytes(), ct);

        var forwardPacket = new TalkForwardNotify(connection.CharacterId, chatRequest.DistID, chatRequest.Message, chatRequest.BalloonID);
        byte[] broadcastData = forwardPacket.ToBytes();

        foreach (var client in state.MsgClients.Values)
        {
            if (client.IsAuthenticated && client.Id != connection.Id)
            {
                await client.SendAsync(PacketType.TalkForwardNotify, broadcastData, ct);
            }
        }
    }
}
