using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Packets.Area;
using Microsoft.Extensions.Logging;

namespace AISpace.Common.Handlers.Area;

public class ItemEquipEndHandler(ILogger<ItemEquipEndHandler> logger) : IPacketHandler, IRequiresAuthenticatedSession
{
    private readonly ILogger<ItemEquipEndHandler> _logger = logger;

    public PacketType RequestType => PacketType.ItemEquipEndRequest;
    public PacketType ResponseType => PacketType.ItemEquipEndResponse;

    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, IPlayerSession session, CancellationToken ct = default)
    {
        var request = ItemEquipEndRequest.FromBytes(payload.Span);
        _logger.LogInformation("Client {Id} requested ItemEquipEnd for ObjId: {ObjId}", session.ConnectionId, request.ObjId);

        var response = new ItemEquipEndResponse(1);
        await session.SendAsync(ResponseType, response.ToBytes(), ct);

        var equipEnded = new ItemEquipEnded(request.ObjId);
        await session.SendAsync(PacketType.ItemEquipEnded, equipEnded.ToBytes(), ct);
    }
}
