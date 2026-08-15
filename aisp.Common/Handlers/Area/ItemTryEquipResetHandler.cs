using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Data;
using aisp.Network.Packets.Area;
using Microsoft.Extensions.Logging;

namespace aisp.Common.Handlers.Area;

public class ItemTryEquipResetHandler(
    ICharacterRepository characterRepo,
    IRoboRepository roboRepository,
    ILogger<ItemTryEquipResetHandler> logger
) : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.ItemTryEquipResetRequest;
    public PacketType ResponseType => PacketType.ItemTryEquipResetResponse;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var request = ItemTryEquipResetRequest.FromBytes(payload.Span);
        logger.LogInformation(
            "Client {Id} requested ItemTryEquipReset for ObjId: {ObjId}",
            session.ConnectionId,
            request.ObjId
        );

        if (session.CharacterId == 0)
        {
            await session.SendAsync(ResponseType, new ItemTryEquipResetResponse(1).ToBytes(), ct);
            return;
        }

        IReadOnlyList<ItemEquipEntry>? equipment = null;
        if (request.ObjId == session.CharacterId)
        {
            var character = await characterRepo.GetByIdAsync(checked((int)session.CharacterId), ct);
            if (character is not null)
                equipment = TryEquipNotifyBuilder.FromCharacter(character);
        }
        else if (RoboRepository.TryGetRoboId(session.CharacterId, request.ObjId, out var roboId))
        {
            var robo = await roboRepository.GetAsync(checked((int)session.CharacterId), roboId, ct);
            if (robo is not null)
                equipment = TryEquipNotifyBuilder.FromRobo(robo);
        }

        if (equipment is null)
        {
            await session.SendAsync(ResponseType, new ItemTryEquipResetResponse(1).ToBytes(), ct);
            return;
        }

        // Cancel (sub_529380): client reverts locally via sub_528770, sends reset, keeps wardrobe open.
        // Echo the persisted target equipment so recv_item_try_equip_replaced re-applies the entry outfit.
        await session.SendAsync(ResponseType, new ItemTryEquipResetResponse(0).ToBytes(), ct);
        await session.SendAsync(
            PacketType.ItemTryEquipReplacedNotify,
            new ItemTryEquipReplacedNotify(request.ObjId, equipment).ToBytes(),
            ct
        );
    }
}
