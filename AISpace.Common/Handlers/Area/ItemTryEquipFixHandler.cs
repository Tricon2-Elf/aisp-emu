using AISpace.Common.DAL.Repositories;
using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Packets.Area;
using Microsoft.Extensions.Logging;

namespace AISpace.Common.Handlers.Area;

public class ItemTryEquipFixHandler(IRoboRepository roboRepository, ILogger<ItemTryEquipFixHandler> logger) : IPacketHandler, IRequiresAuthenticatedSession
{
    private readonly ILogger<ItemTryEquipFixHandler> _logger = logger;

    public PacketType RequestType => PacketType.ItemTryEquipFixRequest;
    public PacketType ResponseType => PacketType.ItemTryEquipFixResponse;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, IPlayerSession session, CancellationToken ct = default)
    {
        var request = ItemTryEquipFixRequest.FromBytes(payload.Span);
        _logger.LogInformation("Client {Id} requested ItemTryEquipFix for ObjId: {ObjId}", session.ConnectionId, request.ObjId);

        var ownsTarget = session.CharacterId != 0 && request.ObjId == session.CharacterId;
        if (!ownsTarget && session.CharacterId != 0 && RoboRepository.TryGetRoboId(session.CharacterId, request.ObjId, out var roboId))
            ownsTarget = await roboRepository.ExistsAsync(checked((int)session.CharacterId), roboId, ct);

        var response = new ItemTryEquipFixResponse(ownsTarget ? 0u : 1u);
        await session.SendAsync(ResponseType, response.ToBytes(), ct);
    }
}
