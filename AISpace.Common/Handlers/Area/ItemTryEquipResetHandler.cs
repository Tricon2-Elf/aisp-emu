using AISpace.Common.Network.Packets.Area;
using AISpace.Network;
using AISpace.Network.Packets.Area;

namespace AISpace.Common.Handlers.Area;

public class ItemTryEquipResetHandler(ILogger<ItemTryEquipResetHandler> logger) : IPacketHandler
{
    private readonly ILogger<ItemTryEquipResetHandler> _logger = logger;

    public PacketType RequestType => PacketType.ItemTryEquipResetRequest;
    public PacketType ResponseType => PacketType.ItemTryEquipResetResponse;
    public MessageDomain Domain => MessageDomain.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, ClientConnection connection, CancellationToken ct = default)
    {
        var request = ItemTryEquipResetRequest.FromBytes(payload.Span);
        _logger.LogInformation("Client {Id} requested ItemTryEquipReset for ObjId: {ObjId}", connection.Id, request.ObjId);

        var response = new ItemTryEquipResetResponse(0);
        await connection.SendAsync(ResponseType, response.ToBytes(), ct);

        var equipEnded = new ItemEquipEnded(request.ObjId);
        await connection.SendAsync(PacketType.ItemEquipEnded, equipEnded.ToBytes(), ct);
    }
}
