using AISpace.Common.Network.Packets.Msg;
using AISpace.Common.Network.Packets.Area;
using AISpace.Common.Game;
using Microsoft.Extensions.Logging;

namespace AISpace.Common.Network.Handlers.Msg;

public class CmdExecHandler(SharedState state, ILogger<CmdExecHandler> logger) : IPacketHandler
{
    public PacketType RequestType => PacketType.CmdExecRequest;
    public PacketType ResponseType => PacketType.CmdExecResponse;
    public MessageDomain Domain => MessageDomain.Msg;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, ClientConnection connection, CancellationToken ct = default)
    {
        var request = CmdExecRequest.FromBytes(payload.Span);
        
        // 1. Обязательный ответ клиенту, что команда принята
        var response = new CmdExecResponse(request.MessageId, 0);
        await connection.SendAsync(ResponseType, response.ToBytes(), ct);

        string cmd = request.Command.ToLower();
        logger.LogInformation($"[CMD] Player {connection.CharacterId} executed: /{cmd}");

        // 2. Логика кнопки Escape
        if (cmd == "escape" || cmd == "reset")
        {
            // Устанавливаем дефолтные координаты карты (обычно центр или вход)
            connection.X = 0f;
            connection.Y = 0.1f;
            connection.Z = 0f;
            connection.Rotation = 0;

            // Создаем пакет перемещения
            var moveData = new MovementData(connection.X, connection.Y, connection.Z, connection.Rotation, MovementType.Stopped);
            var notify = new AvatarNotifyMove(1, connection.CharacterId, moveData).ToBytes();

            // Сообщаем ВСЕМ (включая себя), что мы переместились
            foreach (var client in state.AreaClients.Values)
            {
                await client.SendAsync(PacketType.AvatarNotifyMove, notify, ct);
            }
            
            logger.LogInformation($"[ESCAPE] Player {connection.CharacterId} teleported to start point.");
        }
    }
}