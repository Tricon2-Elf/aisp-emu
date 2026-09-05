using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Packets.Area;

namespace aisp.Common.Handlers.Area;

/// <summary>
/// Creates a client-visible Friend Link placard response.
/// </summary>
public sealed class AreaPlacardSettingHandler(IFriendRepository friends, SharedState state)
    : PacketHandlerBase<PlacardSettingRequest, PlacardSettingResponse>,
        IRequiresAuthenticatedSession
{
    public override PacketType RequestType => PacketType.PlacardSettingRequest;
    public override PacketType ResponseType => PacketType.PlacardSettingResponse;
    public override ServerType ServerType => ServerType.Area;

    public override async Task<PlacardSettingResponse?> HandleAsync(
        PlacardSettingRequest request,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        if (session.CharacterId == 0 || session.CharacterId > int.MaxValue || request.Slot > 4)
            return new PlacardSettingResponse(1);

        var name = session.Character?.Name ?? string.Empty;
        var tagId = request.Slot + 1;
        var tags = await friends.GetLinkTagsAsync((int)session.CharacterId, ct);
        var tagName = tags.FirstOrDefault(x => x.Slot == request.Slot)?.Name ?? string.Empty;
        var (placard, previous) = state.SetFriendLinkPlacard(
            session.UserId,
            session.CharacterId,
            name,
            session.MapId,
            session.ChannelId,
            session.MyRoomId,
            request.Type,
            tagId,
            request.Slot,
            request.Direction,
            tagName,
            request.Position
        );

        if (previous is not null)
        {
            var remove = new NotifyPlacardRemove(previous.PlacardId).ToBytes();
            foreach (var viewer in GetViewers(state, previous))
                await viewer.SendAsync(PacketType.NotifyPlacardRemove, remove, ct);
        }

        var response = new PlacardSettingResponse(
            result: 0,
            placardId: placard.PlacardId,
            ownerName: name,
            ownerAvatarId: session.CharacterId,
            tagId: tagId,
            slot: request.Slot,
            direction: request.Direction,
            tagName: tagName,
            position: request.Position
        );
        // The response records the owner's active placard id. The following notify is
        // what creates the actual 3D object, so it must also be sent to the owner.
        await session.SendAsync(ResponseType, response.ToBytes(), ct);

        var notify = new NotifyPlacardSetting(
            placard.PlacardId,
            name,
            session.CharacterId,
            tagId,
            request.Slot,
            request.Direction,
            tagName,
            request.Position
        ).ToBytes();
        await session.SendAsync(PacketType.NotifyPlacardSetting, notify, ct);
        foreach (var peer in state.GetAreaPeers(session))
            await peer.SendAsync(PacketType.NotifyPlacardSetting, notify, ct);

        // Response was sent before the notify to preserve the retail client's expected order.
        return null;
    }

    private static IEnumerable<IPlayerSession> GetViewers(
        SharedState state,
        ActiveFriendLinkPlacard placard
    )
    {
        var sessions = state.GetAreaSessions(placard.MapId, placard.ChannelId);
        return MyRoomInfo.IsMyRoomMap(placard.MapId)
            ? sessions.Where(x => x.MyRoomId == placard.MyRoomId)
            : sessions;
    }
}
