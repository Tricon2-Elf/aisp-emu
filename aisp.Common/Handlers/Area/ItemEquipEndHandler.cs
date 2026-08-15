using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Packets.Area;
using Microsoft.Extensions.Logging;

namespace aisp.Common.Handlers.Area;

public class ItemEquipEndHandler(
    IRoboRepository roboRepository,
    ILogger<ItemEquipEndHandler> logger
) : IPacketHandler, IRequiresAuthenticatedSession
{
    private readonly ILogger<ItemEquipEndHandler> _logger = logger;

    public PacketType RequestType => PacketType.ItemEquipEndRequest;
    public PacketType ResponseType => PacketType.ItemEquipEndResponse;

    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var request = ItemEquipEndRequest.FromBytes(payload.Span);
        _logger.LogInformation(
            "Client {Id} requested ItemEquipEnd for ObjId: {ObjId}",
            session.ConnectionId,
            request.ObjId
        );

        var ownsTarget = session.CharacterId != 0 && request.ObjId == session.CharacterId;
        if (
            !ownsTarget
            && session.CharacterId != 0
            && RoboRepository.TryGetRoboId(session.CharacterId, request.ObjId, out var roboId)
        )
            ownsTarget = await roboRepository.ExistsAsync(
                checked((int)session.CharacterId),
                roboId,
                ct
            );

        // Client sub_78A890→sub_5295A0 commits wardrobe changes only when result==0.
        var response = new ItemEquipEndResponse(ownsTarget ? 0u : 1u);
        await session.SendAsync(ResponseType, response.ToBytes(), ct);
        if (!ownsTarget)
            return;

        var equipEnded = new ItemEquipEnded(request.ObjId);
        await session.SendAsync(PacketType.ItemEquipEnded, equipEnded.ToBytes(), ct);
    }
}
