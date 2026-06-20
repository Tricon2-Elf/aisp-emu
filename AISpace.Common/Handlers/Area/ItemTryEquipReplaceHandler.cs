using AISpace.Common.DAL.Repositories;
using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Packets.Area;
using Microsoft.Extensions.Logging;

namespace AISpace.Common.Handlers.Area;

public class ItemTryEquipReplaceHandler(ICharacterRepository characterRepo, ILogger<ItemTryEquipReplaceHandler> logger) : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.ItemTryEquipReplaceRequest;
    public PacketType ResponseType => PacketType.ItemTryEquipReplaceResponse;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, IPlayerSession session, CancellationToken ct = default)
    {
        var request = ItemTryEquipReplaceRequest.FromBytes(payload.Span);
        logger.LogInformation(
            "Client {ConnectionId} ItemTryEquipReplace objId={ObjId} equipCount={Count}",
            session.ConnectionId,
            request.ObjId,
            request.Equips.Count
        );

        if (session.CharacterId != 0)
            await characterRepo.ReplaceEquipmentAsync((int)session.CharacterId, request.Equips, ct);

        await session.SendAsync(PacketType.ItemTryEquipReplaceResponse, new ItemTryEquipReplaceResponse(0).ToBytes(), ct);
        await session.SendAsync(
            PacketType.ItemTryEquipReplacedNotify,
            new ItemTryEquipReplacedNotify(request.ObjId, request.Equips).ToBytes(),
            ct
        );
    }
}
