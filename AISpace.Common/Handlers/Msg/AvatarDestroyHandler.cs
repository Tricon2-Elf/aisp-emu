using AISpace.Common.DAL;
using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Packets.Msg;
using Microsoft.Extensions.Logging;

namespace AISpace.Common.Handlers.Msg;

public class AvatarDestroyHandler(MainContext db, ILogger<AvatarDestroyHandler> logger) : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.AvatarDestroyRequest;
    public PacketType ResponseType => PacketType.AvatarDestroyResponse;
    public ServerType ServerType => ServerType.Msg;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, IPlayerSession session, CancellationToken ct = default)
    {
        // Find the first character (as the emulator currently supports only one)
        var cha = session.User!.Characters.FirstOrDefault();

        if (cha != null)
        {
            logger.LogInformation($"[DELETE] Removing character '{cha.Name}' for User {session.User.Username}");

            // 1. Remove from database
            db.Characters.Remove(cha);
            await db.SaveChangesAsync(ct);

            // 2. Clear from memory of the current session
            session.User.Characters.Remove(cha);
        }

        // 3. Respond to the client, that everything is OK
        var response = new AvatarDestroyResponse(0);
        await session.SendAsync(ResponseType, response.ToBytes(), ct);
    }
}
