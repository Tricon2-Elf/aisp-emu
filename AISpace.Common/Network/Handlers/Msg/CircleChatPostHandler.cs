using AISpace.Common.Network.Packets.Msg;
using AISpace.Common.Game;
using Microsoft.Extensions.Logging;

namespace AISpace.Common.Network.Handlers.Msg;

public class CircleChatPostHandler(ILogger<CircleChatPostHandler> logger, SharedState state) : IPacketHandler
{
    public PacketType RequestType => PacketType.CircleChatPostRequest;
    public PacketType ResponseType => PacketType.CircleChatPostResponse;
    public MessageDomain Domain => MessageDomain.Msg;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, ClientConnection connection, CancellationToken ct = default)
    {
        // Читаем запрос (CircleID, Message, BalloonID)
        var reader = new PacketReader(payload.Span);
        uint circleId = reader.ReadUInt();
        string message = reader.ReadString("Shift_JIS");
        uint balloonId = reader.ReadUInt();

        logger.LogInformation($"[CIRCLE CHAT] From:{connection.CharacterId} Circle:{circleId}: {message}");

        // 1. Ответ отправителю
        var response = new CmdExecResponse(0, 0); 
        await connection.SendAsync(ResponseType, response.ToBytes(), ct);

        // 2. Подготовка рассылки (recv_circle_chat_forward)
        var writer = new PacketWriter();
        writer.Write(circleId);           // ID гильдии
        writer.Write(connection.CharacterId); // Кто отправил
        writer.Write(message, "Shift_JIS"); // Текст + \0
        writer.Write(balloonId);          // Тип облачка

        byte[] forwardData = writer.ToBytes();

        // 3. Рассылка согильдийцам (кроме себя)
        foreach (var client in state.MsgClients.Values)
        {
            if (client.IsAuthenticated && client.Id != connection.Id)
            {
                // Тут в будущем нужна проверка: if (client.InCircle == circleId)
                await client.SendAsync(PacketType.CircleChatForwardNotify, forwardData, ct);
            }
        }
    }
}