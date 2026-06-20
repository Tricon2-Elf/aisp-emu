using AISpace.Common.DAL.Repositories;
using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Packets.Area;
using Microsoft.Extensions.Logging;

namespace AISpace.Common.Handlers.Area;

public class ItemGetListHandler(ICharacterRepository characterRepo, ILogger<ItemGetListHandler> logger) : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.ItemGetListRequest;
    public PacketType ResponseType => PacketType.ItemGetListResponse;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, IPlayerSession session, CancellationToken ct = default)
    {
        logger.LogInformation("Client {Id} requested ItemGetList", session.ConnectionId);

        if (session.CharacterId == 0)
        {
            await session.SendAsync(ResponseType, new ItemGetListResponse(0).ToBytes(), ct);
            return;
        }

        var character = session.Character ?? await characterRepo.GetByIdAsync((int)session.CharacterId, ct);
        if (character is null)
        {
            await session.SendAsync(ResponseType, new ItemGetListResponse(0).ToBytes(), ct);
            return;
        }

        session.Character = character;
        await CharacterItemSync.SendInventoryBootstrapAsync(session, character, ct);
    }
}
