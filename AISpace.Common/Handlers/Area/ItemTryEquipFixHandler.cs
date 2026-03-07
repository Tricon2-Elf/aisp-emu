using AISpace.Network;
using AISpace.Network.Packets.Area;

namespace AISpace.Common.Handlers.Area;

public class ItemTryEquipFixHandler(ILogger<ItemTryEquipFixHandler> logger) : IPacketHandler
{
    private readonly ILogger<ItemTryEquipFixHandler> _logger = logger;

    public PacketType RequestType => PacketType.ItemTryEquipFixRequest;
    public PacketType ResponseType => PacketType.ItemTryEquipFixResponse;
    public MessageDomain Domain => MessageDomain.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, ClientConnection connection, CancellationToken ct = default)
    {
        var request = ItemTryEquipFixRequest.FromBytes(payload.Span);
        _logger.LogInformation("Client {Id} requested ItemTryEquipFix for ObjId: {ObjId}", connection.Id, request.ObjId);

        var response = new ItemTryEquipFixResponse(0);
        await connection.SendAsync(ResponseType, response.ToBytes(), ct);

        var equipEnded = new ItemEquipEnded(request.ObjId);
        await connection.SendAsync(PacketType.ItemEquipEnded, equipEnded.ToBytes(), ct);
    }
}
