using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Packets.Area;

namespace aisp.Common.Handlers.Area;

/// <summary>Accepts removal of the current player's Friend Link placard.</summary>
public sealed class AreaPlacardRemoveHandler(SharedState state)
    : PacketHandlerBase<PlacardRemoveRequest, PlacardRemoveResponse>,
        IRequiresAuthenticatedSession
{
    public override PacketType RequestType => PacketType.PlacardRemoveRequest;
    public override PacketType ResponseType => PacketType.PlacardRemoveResponse;
    public override ServerType ServerType => ServerType.Area;

    public override async Task<PlacardRemoveResponse?> HandleAsync(
        PlacardRemoveRequest request,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        if (session.CharacterId == 0)
            return new PlacardRemoveResponse(1);

        if (state.TryRemoveFriendLinkPlacard(session.CharacterId, out var removed))
        {
            var notify = new NotifyPlacardRemove(removed!.PlacardId).ToBytes();
            var viewers = state.GetAreaSessions(removed.MapId, removed.ChannelId);
            if (MyRoomInfo.IsMyRoomMap(removed.MapId))
                viewers = [.. viewers.Where(x => x.MyRoomId == removed.MyRoomId)];
            foreach (var viewer in viewers.Where(x => x.ConnectionId != session.ConnectionId))
                await viewer.SendAsync(PacketType.NotifyPlacardRemove, notify, ct);
        }

        return new PlacardRemoveResponse(0);
    }
}
