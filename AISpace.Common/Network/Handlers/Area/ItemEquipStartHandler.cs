using AISpace.Common.Network.Packets.Area;
using Microsoft.Extensions.Logging;

namespace AISpace.Common.Network.Handlers.Area;

public class ItemEquipStartHandler(ILogger<ItemEquipStartHandler> logger) : IPacketHandler
{
    public PacketType RequestType => PacketType.ItemEquipStartRequest;

    public PacketType ResponseType => PacketType.ItemEquipStartResponse;

    public MessageDomain Domain => MessageDomain.Area;

    private readonly ILogger<ItemEquipStartHandler> _logger = logger;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, ClientConnection connection, CancellationToken ct = default)
    {
        var request = ItemEquipStartRequest.FromBytes(payload.Span);
        _logger.LogInformation("Client {Id} requested ItemEquipStart for ObjId: {ObjId}", connection.Id, request.ObjId);

        var response = new ItemEquipStartResponse(1);
        await connection.SendAsync(ResponseType, response.ToBytes(), ct);

        var forceStarted = new ItemEquipForceStarted(request.ObjId);
        await connection.SendAsync(PacketType.ItemEquipForceStarted, forceStarted.ToBytes(), ct);
    }
}
