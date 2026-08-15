using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Packets.Area;
using Microsoft.Extensions.Logging;

namespace aisp.Common.Handlers.Area;

public class ItemGetListHandler(
    ICharacterRepository characterRepo,
    IUserRepository userRepo,
    ILogger<ItemGetListHandler> logger
) : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.ItemGetListRequest;
    public PacketType ResponseType => PacketType.ItemGetListResponse;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        logger.LogInformation("Client {Id} requested ItemGetList", session.ConnectionId);

        if (session.CharacterId == 0)
        {
            await session.SendAsync(ResponseType, new ItemGetListResponse(0).ToBytes(), ct);
            return;
        }

        var character =
            session.Character ?? await characterRepo.GetByIdAsync((int)session.CharacterId, ct);
        if (character is null)
        {
            await session.SendAsync(ResponseType, new ItemGetListResponse(0).ToBytes(), ct);
            return;
        }

        session.Character = character;

        IEnumerable<(int ItemId, int Quantity)> storageItems = [];
        if (session.User is not null)
        {
            var stored = await userRepo.GetStorageItemsAsync(session.User.Id, ct);
            storageItems = stored.Select(x => (x.ItemId, x.Quantity));
        }

        await CharacterItemSync.SendInventoryBootstrapAsync(session, character, storageItems, ct);
    }
}
