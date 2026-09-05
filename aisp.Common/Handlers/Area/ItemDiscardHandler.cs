using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Packets.Area;
using Microsoft.Extensions.Logging;

namespace aisp.Common.Handlers.Area;

/// <summary>
/// send_item_discard (0xED61) → recv_item_discard_r (0x2546). The bag's 捨てる option: the item window
/// (IF::CItemWindow slot 95) sends the selected serial and the quantity chosen in the count dialog.
/// Bag contents are pushed with item_update_num / item_delete before the result.
/// </summary>
public class ItemDiscardHandler(
    ICharacterRepository characterRepo,
    ILogger<ItemDiscardHandler> logger
) : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.ItemDiscardRequest;
    public PacketType ResponseType => PacketType.ItemDiscardResponse;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        ItemDiscardRequest request;
        try
        {
            request = ItemDiscardRequest.FromBytes(payload.Span);
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Malformed send_item_discard from character {CharacterId}",
                session.CharacterId
            );
            await session.SendAsync(ResponseType, new ItemDiscardResponse(1).ToBytes(), ct);
            return;
        }

        var ok = await InventoryDiscardFlow.DiscardAsync(
            characterRepo,
            session,
            [(request.SerialId, request.Num)],
            logger,
            ct
        );
        await session.SendAsync(ResponseType, new ItemDiscardResponse(ok ? 0u : 1u).ToBytes(), ct);
    }
}
