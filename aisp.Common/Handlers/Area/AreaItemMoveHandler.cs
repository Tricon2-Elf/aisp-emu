using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Packets.Area;
using Microsoft.Extensions.Logging;

namespace aisp.Common.Handlers.Area;

/// <summary>
/// send_item_move (0x8C7C) → recv_item_move_r (0x708B).
/// Wardrobe warehouse moves between inventory place=0 and account storage place=1.
/// </summary>
public sealed class AreaItemMoveHandler(
    IUserRepository userRepo,
    ILogger<AreaItemMoveHandler> logger
) : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.ItemMoveRequest;
    public PacketType ResponseType => PacketType.ItemMoveResponse;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        if (session.User is null || session.CharacterId == 0)
        {
            await session.SendAsync(ResponseType, new ItemMoveResponse(1).ToBytes(), ct);
            return;
        }

        ItemMoveRequest request;
        try
        {
            request = ItemMoveRequest.FromBytes(payload.Span);
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Failed to parse ItemMoveRequest for character {CharacterId}",
                session.CharacterId
            );
            await session.SendAsync(ResponseType, new ItemMoveResponse(1).ToBytes(), ct);
            return;
        }

        var inventoryPlace = CharacterItemSync.PrimaryItemTablePlace;
        var storagePlace = CharacterItemSync.StorageItemTablePlace;
        bool toStorage;
        if (request.FromPlace == inventoryPlace && request.ToPlace == storagePlace)
            toStorage = true;
        else if (request.FromPlace == storagePlace && request.ToPlace == inventoryPlace)
            toStorage = false;
        else
        {
            logger.LogWarning(
                "Unsupported item move places {From}->{To} for character {CharacterId}",
                request.FromPlace,
                request.ToPlace,
                session.CharacterId
            );
            await session.SendAsync(ResponseType, new ItemMoveResponse(1).ToBytes(), ct);
            return;
        }

        if (request.Num == 0 || request.SerialId == 0 || request.SerialId > int.MaxValue)
        {
            await session.SendAsync(ResponseType, new ItemMoveResponse(1).ToBytes(), ct);
            return;
        }

        var itemId = (int)request.SerialId;
        var result = await userRepo.TransferStorageItemAsync(
            session.User.Id,
            (int)session.CharacterId,
            itemId,
            request.Num,
            toStorage,
            ct
        );
        if (result is null)
        {
            logger.LogWarning(
                "ItemMove failed: item {ItemId} char {CharacterId} userId {UserId} qty {Qty} toStorage={ToStorage}",
                itemId,
                session.CharacterId,
                session.User.Id,
                request.Num,
                toStorage
            );
            await session.SendAsync(ResponseType, new ItemMoveResponse(1).ToBytes(), ct);
            return;
        }

        var (inventoryQuantity, storageQuantity) = result.Value;
        await CharacterItemSync.SyncItemTableQuantityAsync(
            session,
            inventoryPlace,
            itemId,
            inventoryQuantity,
            ct
        );
        await CharacterItemSync.SyncItemTableQuantityAsync(
            session,
            storagePlace,
            itemId,
            storageQuantity,
            ct
        );
        await session.SendAsync(ResponseType, new ItemMoveResponse(0).ToBytes(), ct);
    }
}
