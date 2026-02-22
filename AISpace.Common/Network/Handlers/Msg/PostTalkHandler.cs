using AISpace.Common.Network.Packets;
using AISpace.Common.Network.Packets.Msg;
using AISpace.Common.Game;
using Microsoft.Extensions.Logging;

namespace AISpace.Common.Network.Handlers.Msg;

public class PostTalkHandler(ILogger<PostTalkHandler> logger, SharedState state) : IPacketHandler
{
    public PacketType RequestType => PacketType.PostTalkRequest;
    public PacketType ResponseType => PacketType.PostTalkResponse;
    public MessageDomain Domain => MessageDomain.Msg;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, ClientConnection connection, CancellationToken ct = default)
    {
        var chatRequest = PostTalkRequest.FromBytes(payload.Span);
        
        // 1. Отвечаем отправителю (ОК)
        var response = new PostTalkResponse(chatRequest.MessageID, 0);
        await connection.SendAsync(ResponseType, response.ToBytes(), ct);

        // 2. Подготовка пакета для других
        var forwardPacket = new TalkForwardNotify(
            connection.CharacterId, 
            chatRequest.DistID,     
            chatRequest.Message,    
            chatRequest.BalloonID   
        );
        byte[] broadcastData = forwardPacket.ToBytes();

        // 3. Рассылаем ВСЕМ, кроме себя
        foreach (var client in state.MsgClients.Values)
        {
            // ИСКЛЮЧАЕМ СЕБЯ (client.Id != connection.Id), чтобы не было дублей
            if (client.IsAuthenticated && client.Id != connection.Id)
            {
                await client.SendAsync(PacketType.TalkForwardNotify, broadcastData, ct);
            }
        }
    }
}