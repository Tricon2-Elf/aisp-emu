using AISpace.Common.Network.Packets.Msg;
using AISpace.Network;

namespace AISpace.Common.Handlers.Msg;

public class AvatarDestroyHandler(MainContext db, ILogger<AvatarDestroyHandler> logger) : IPacketHandler
{
    public PacketType RequestType => PacketType.AvatarDestroyRequest;
    public PacketType ResponseType => PacketType.AvatarDestroyResponse;
    public MessageDomain Domain => MessageDomain.Msg;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, ClientConnection connection, CancellationToken ct = default)
    {
        if (connection.User == null)
            return;

        // Find the first character (as the emulator currently supports only one)
        var cha = connection.User.Characters.FirstOrDefault();

        if (cha != null)
        {
            logger.LogInformation($"[DELETE] Removing character '{cha.Name}' for User {connection.User.Username}");

            // 1. Remove from database
            db.Characters.Remove(cha);
            await db.SaveChangesAsync(ct);

            // 2. Clear from memory of the current session
            connection.User.Characters.Remove(cha);
        }

        // 3. Respond to the client, that everything is OK
        var response = new AvatarDestroyResponse(0);
        await connection.SendAsync(ResponseType, response.ToBytes(), ct);
    }
}
