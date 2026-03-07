using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Packets.Area;
using Microsoft.Extensions.Logging;

namespace AISpace.Common.Handlers.Area;

public class ItemEquipStartHandler(ILogger<ItemEquipStartHandler> logger) : IPacketHandler
{
    public PacketType RequestType => PacketType.ItemEquipStartRequest;

    public PacketType ResponseType => PacketType.ItemEquipStartResponse;

    public MessageDomain Domain => MessageDomain.Area;

    private readonly ILogger<ItemEquipStartHandler> _logger = logger;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, IPlayerSession session, CancellationToken ct = default)
    {
        var request = ItemEquipStartRequest.FromBytes(payload.Span);
        _logger.LogInformation("Client {Id} requested ItemEquipStart for ObjId: {ObjId}", session.ConnectionId, request.ObjId);

        var response = new ItemEquipStartResponse(1);
        await session.SendAsync(ResponseType, response.ToBytes(), ct);

        var forceStarted = new ItemEquipForceStarted(request.ObjId);
        await session.SendAsync(PacketType.ItemEquipForceStarted, forceStarted.ToBytes(), ct);
    }
}
