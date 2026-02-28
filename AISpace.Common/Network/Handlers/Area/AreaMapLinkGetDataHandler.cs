using AISpace.Common.Network.Packets.Area;
using Microsoft.Extensions.Logging;

namespace AISpace.Common.Network.Handlers;

public class AreaMapLinkGetDataHandler(ILogger<AreaMapLinkGetDataHandler> logger) : IPacketHandler
{
    public PacketType RequestType => PacketType.MapLinkGetDataRequest;

    public PacketType ResponseType => PacketType.MapLinkGetDataResponse;

    public MessageDomain Domain => MessageDomain.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, ClientConnection connection, CancellationToken ct = default)
    {
        var request = MapLinkGetDataRequest.FromBytes(payload.Span);
        logger.LogCritical("MapLinkGetDataRequest received from user {UserId} on map {MapId} with channel {ChannelId}", connection.User.Id, request.MapId, request.ChannelId);
        var response = new MapLinkGetDataResponse(1);
        await connection.SendAsync(ResponseType, response.ToBytes(), ct);
        var maplinkAtPlayer = new MapLinkData(connection.X, connection.Y, connection.Z - 1000f, 0, 1000f, 10f);
        await connection.SendAsync(PacketType.MapLinkNotifyData, new MapLinkNotifyData(0, maplinkAtPlayer).ToBytes(), ct);
        // Tell client where this maplink goes (same order as maplinks)
        await connection.SendAsync(PacketType.NotifySelectMap, new NotifySelectMapData(10990110u).ToBytes(), ct);
    }
}
