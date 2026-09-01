using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Packets.Area;
using Microsoft.Extensions.Logging;

namespace aisp.Common.Handlers.Area;

/// <summary>
/// send_trashbox_discard_item (0xB18E) → recv_trashbox_discard_item_r (0xBBEB). The trashbox window
/// (opened via send_trashbox_open) collects up to ten stacks; this packet carries them all, and after
/// the _r the client itself sends send_trashbox_close. Bag contents are pushed with
/// item_update_num / item_delete before the result.
/// </summary>
public class AreaTrashboxDiscardItemHandler(
    ICharacterRepository characterRepo,
    ILogger<AreaTrashboxDiscardItemHandler> logger
) : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.TrashboxDiscardItemRequest;
    public PacketType ResponseType => PacketType.TrashboxDiscardItemResponse;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        TrashboxDiscardItemRequest request;
        try
        {
            request = TrashboxDiscardItemRequest.FromBytes(payload.Span);
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Malformed send_trashbox_discard_item from character {CharacterId}",
                session.CharacterId
            );
            await session.SendAsync(ResponseType, new TrashboxDiscardItemResponse(1).ToBytes(), ct);
            return;
        }

        if (request.SerialIds.Count != request.Nums.Count)
        {
            logger.LogWarning(
                "Trashbox discard from character {CharacterId} has {Serials} serials but {Nums} counts",
                session.CharacterId,
                request.SerialIds.Count,
                request.Nums.Count
            );
            await session.SendAsync(ResponseType, new TrashboxDiscardItemResponse(1).ToBytes(), ct);
            return;
        }

        var ok = await InventoryDiscardFlow.DiscardAsync(
            characterRepo,
            session,
            request.SerialIds.Zip(request.Nums),
            logger,
            ct
        );
        await session.SendAsync(
            ResponseType,
            new TrashboxDiscardItemResponse(ok ? 0u : 1u).ToBytes(),
            ct
        );
    }
}
