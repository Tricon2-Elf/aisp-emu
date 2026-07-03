using AISpace.Common.DAL.Repositories;
using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Packets.Area;
using Microsoft.Extensions.Logging;

namespace AISpace.Common.Handlers.Area;

public class ItemTryEquipResetHandler(ICharacterRepository characterRepo, ILogger<ItemTryEquipResetHandler> logger)
    : IPacketHandler,
        IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.ItemTryEquipResetRequest;
    public PacketType ResponseType => PacketType.ItemTryEquipResetResponse;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, IPlayerSession session, CancellationToken ct = default)
    {
        var request = ItemTryEquipResetRequest.FromBytes(payload.Span);
        logger.LogInformation("Client {Id} requested ItemTryEquipReset for ObjId: {ObjId}", session.ConnectionId, request.ObjId);

        // Cancel (sub_529380): client reverts locally via sub_528770, sends reset, keeps wardrobe open.
        // reset_r (sub_529E80) clears the in-flight wait (5342) via sub_526F10 — it does NOT close the UI.
        // If try-on updated the live CChara equip, local revert can be incomplete; echo saved DB equips
        // through replaced so recv_item_try_equip_replaced re-applies the entry outfit (sub_5296C0).
        await session.SendAsync(ResponseType, new ItemTryEquipResetResponse(0).ToBytes(), ct);

        if (session.CharacterId != 0)
        {
            var character = await characterRepo.GetByIdAsync((int)session.CharacterId, ct);
            if (character is not null)
            {
                var equips = TryEquipNotifyBuilder.FromCharacter(character);
                if (equips.Count > 0)
                {
                    await session.SendAsync(
                        PacketType.ItemTryEquipReplacedNotify,
                        new ItemTryEquipReplacedNotify(request.ObjId, equips).ToBytes(),
                        ct
                    );
                }
            }
        }
    }
}
