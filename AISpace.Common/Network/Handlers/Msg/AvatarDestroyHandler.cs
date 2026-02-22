using AISpace.Common.DAL;
using AISpace.Common.Network.Packets.Msg;
using Microsoft.Extensions.Logging;

namespace AISpace.Common.Network.Handlers.Msg;

public class AvatarDestroyHandler(MainContext db, ILogger<AvatarDestroyHandler> logger) : IPacketHandler
{
    public PacketType RequestType => PacketType.AvatarDestroyRequest;
    public PacketType ResponseType => PacketType.AvatarDestroyResponse;
    public MessageDomain Domain => MessageDomain.Msg;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, ClientConnection connection, CancellationToken ct = default)
    {
        if (connection.User == null) return;

        // Находим первого персонажа (так как эмулятор пока поддерживает одного)
        var cha = connection.User.Characters.FirstOrDefault();

        if (cha != null)
        {
            logger.LogInformation($"[DELETE] Removing character '{cha.Name}' for User {connection.User.Username}");
            
            // 1. Удаляем из базы данных
            db.Characters.Remove(cha);
            await db.SaveChangesAsync(ct);

            // 2. Очищаем из памяти текущей сессии
            connection.User.Characters.Remove(cha);
        }

        // 3. Отвечаем клиенту, что всё Ок
        var response = new AvatarDestroyResponse(0);
        await connection.SendAsync(ResponseType, response.ToBytes(), ct);
    }
}