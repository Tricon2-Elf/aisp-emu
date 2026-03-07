using AISpace.Network.Packets.Area;
using AISpace.Network;
using AISpace.Common.Game;
using Microsoft.Extensions.Logging;

namespace AISpace.Common.Handlers.Area;

public class AreaMapLinkGetDataHandler(ILogger<AreaMapLinkGetDataHandler> logger) : IPacketHandler
{
    public PacketType RequestType => PacketType.MapLinkGetDataRequest;

    public PacketType ResponseType => PacketType.MapLinkGetDataResponse;

    public MessageDomain Domain => MessageDomain.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, IPlayerSession session, CancellationToken ct = default)
    {
        var request = MapLinkGetDataRequest.FromBytes(payload.Span);
        logger.LogCritical("MapLinkGetDataRequest received from user {UserId} on map {MapId} with channel {ChannelId}", session.User!.Id, request.MapId, request.ChannelId);
        var response = new MapLinkGetDataResponse(0);
        await session.SendAsync(ResponseType, response.ToBytes(), ct);
        var maplinkAtPlayer = new MapLinkData(session.X, session.Y, session.Z, 0, 1000f, 1000f);
        //await session.SendAsync(PacketType.MapLinkNotifyData, new MapLinkNotifyData(0, maplinkAtPlayer).ToBytes(), ct);
        // Tell client where this maplink goes (same order as maplinks)
        //await session.SendAsync(PacketType.NotifySelectMap, new NotifySelectMapData(10990110u).ToBytes(), ct);
    }
}
