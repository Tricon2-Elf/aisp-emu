using AISpace.Common.Network.Packets.Area;
using AISpace.Network;
using AISpace.Network.Packets.Area;

namespace AISpace.Common.Handlers.Area;

public class ItemEquipEndHandler(ILogger<ItemEquipEndHandler> logger) : IPacketHandler
{
    private readonly ILogger<ItemEquipEndHandler> _logger = logger;

    public PacketType RequestType => PacketType.ItemEquipEndRequest;
    public PacketType ResponseType => PacketType.ItemEquipEndResponse;

    public MessageDomain Domain => MessageDomain.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, ClientConnection connection, CancellationToken ct = default)
    {
        var request = ItemEquipEndRequest.FromBytes(payload.Span);
        _logger.LogInformation("Client {Id} requested ItemEquipEnd for ObjId: {ObjId}", connection.Id, request.ObjId);

        var response = new ItemEquipEndResponse(1);
        await connection.SendAsync(ResponseType, response.ToBytes(), ct);

        var equipEnded = new ItemEquipEnded(request.ObjId);
        await connection.SendAsync(PacketType.ItemEquipEnded, equipEnded.ToBytes(), ct);
    }
}
