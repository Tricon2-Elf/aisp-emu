using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Packets.Area;
using Microsoft.Extensions.Logging;

namespace AISpace.Common.Handlers.Area;

public class ItemTryEquipFixHandler(ILogger<ItemTryEquipFixHandler> logger) : IPacketHandler, IRequiresAuthenticatedSession
{
    private readonly ILogger<ItemTryEquipFixHandler> _logger = logger;

    public PacketType RequestType => PacketType.ItemTryEquipFixRequest;
    public PacketType ResponseType => PacketType.ItemTryEquipFixResponse;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, IPlayerSession session, CancellationToken ct = default)
    {
        var request = ItemTryEquipFixRequest.FromBytes(payload.Span);
        _logger.LogInformation("Client {Id} requested ItemTryEquipFix for ObjId: {ObjId}", session.ConnectionId, request.ObjId);

        var response = new ItemTryEquipFixResponse(0);
        await session.SendAsync(ResponseType, response.ToBytes(), ct);
    }
}
