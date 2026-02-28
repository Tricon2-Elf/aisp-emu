using AISpace.Common.Network.Packets.Area;
using Microsoft.Extensions.Logging;

namespace AISpace.Common.Network.Handlers;

public class AreaMapEnterHandler(ILogger<AreaMapEnterHandler> logger) : IPacketHandler
{
    public PacketType RequestType => PacketType.MapEnterRequest;
    public PacketType ResponseType => PacketType.MapEnterResponse;
    public MessageDomain Domain => MessageDomain.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, ClientConnection connection, CancellationToken ct = default)
    {
        var request = AreaMapEnterRequest.FromBytes(payload.Span);
        logger.LogWarning("MapEnterRequest from user {UserId}: requested MapID {MapId}", connection.User.Id, request.MapID);
        var response = new AreaMapEnterResponse(0);
        await connection.SendAsync(ResponseType, response.ToBytes(), ct);
    }
}
