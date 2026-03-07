using AISpace.Network;
using AISpace.Network.Packets.Area;
using AISpace.Common.Game;
using Microsoft.Extensions.Logging;

namespace AISpace.Common.Handlers.Area;

public class AreaMapEnterHandler(ILogger<AreaMapEnterHandler> logger) : IPacketHandler
{
    public PacketType RequestType => PacketType.MapEnterRequest;
    public PacketType ResponseType => PacketType.MapEnterResponse;
    public MessageDomain Domain => MessageDomain.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, IPlayerSession session, CancellationToken ct = default)
    {
        var request = AreaMapEnterRequest.FromBytes(payload.Span);
        logger.LogInformation("MapEnterRequest from user {UserId}: requested MapID {MapId}", session.User.Id, request.MapID);
        var response = new AreaMapEnterResponse(0);
        await session.SendAsync(ResponseType, response.ToBytes(), ct);
    }
}
