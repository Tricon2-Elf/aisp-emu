using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Data;
using aisp.Network.Packets.Area;

namespace aisp.Common.Handlers.Area;

public sealed class AreaNicotvOpenByFurnitureHandler(
    INicotvRepository nicotvRepository,
    SharedState state
)
    : PacketHandlerBase<NicotvOpenByFurnitureRequest, NicotvOpenResponse>,
        IRequiresAuthenticatedSession
{
    public override PacketType RequestType => PacketType.NicotvOpenByFurnitureRequest;
    public override PacketType ResponseType => PacketType.NicotvOpenResponse;
    public override ServerType ServerType => ServerType.Area;

    public override async Task<NicotvOpenResponse?> HandleAsync(
        NicotvOpenByFurnitureRequest request,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        if (
            !MyRoomInfo.IsMyRoomMap(session.MapId)
            || session.MyRoomId == 0
            || session.MyRoomId > int.MaxValue
        )
            return new NicotvOpenResponse(request.FurnitureId, 0, new NicotvData());

        var roomId = checked((int)session.MyRoomId);
        var existing = await nicotvRepository.GetOrCreateForFurnitureAsync(
            roomId,
            request.FurnitureId,
            ct
        );
        var previousPlayback = existing?.PlaybackState;
        var previousMovieId = existing?.MovieId;
        var previousCommentVisibility = existing?.CommentVisibility;

        var nicotv = await nicotvRepository.UpdateForFurnitureAsync(
            roomId,
            request.FurnitureId,
            request.Nicotv,
            ct
        );
        if (nicotv is null)
            return new NicotvOpenResponse(request.FurnitureId, 0, new NicotvData());

        var nicotvId = checked((uint)nicotv.Id);

        // Client has no dedicated send for comment-visible; open carries the full NicotvData
        // snapshot, so broadcast peer notifies for fields that changed.
        if (previousPlayback is null || previousPlayback != nicotv.PlaybackState)
        {
            await MyRoomFurnitureNotification.BroadcastToRoomAsync(
                state,
                session,
                session.MyRoomId,
                PacketType.NotifyNicotvPlay,
                new NotifyNicotvPlay(nicotvId, (uint)nicotv.PlaybackState).ToBytes(),
                includeSource: false,
                ct
            );
        }

        if (
            previousMovieId is null
            || !string.Equals(previousMovieId, nicotv.MovieId, StringComparison.Ordinal)
        )
        {
            await MyRoomFurnitureNotification.BroadcastToRoomAsync(
                state,
                session,
                session.MyRoomId,
                PacketType.NotifyNicotvSetMovie,
                new NotifyNicotvSetMovie(
                    nicotvId,
                    NicotvMapper.WithNicotvId(nicotv.MovieId, nicotvId)
                ).ToBytes(),
                includeSource: false,
                ct
            );
        }

        if (
            previousCommentVisibility is null
            || previousCommentVisibility != nicotv.CommentVisibility
        )
        {
            await session.SendAsync(
                PacketType.NicotvSetCommentVisibleResponse,
                new NicotvSetCommentVisibleResponse(0, nicotvId).ToBytes(),
                ct
            );
            // Comment visibility has no dedicated set request of its own (confirmed: the client
            // binary has no send for it): the toggle button re-sends open-by-furniture with the
            // new snapshot, diffed above. recv_nicotv_set_comment_visible_r (just sent) does not
            // call the JS setter either, only recv_notify_nicotv_set_comment_visible does, so the
            // clicker needs this notify too, same as set-movie's own _r-is-a-no-op case.
            await MyRoomFurnitureNotification.BroadcastToRoomAsync(
                state,
                session,
                session.MyRoomId,
                PacketType.NotifyNicotvSetCommentVisible,
                new NotifyNicotvSetCommentVisible(
                    nicotvId,
                    (uint)nicotv.CommentVisibility
                ).ToBytes(),
                includeSource: true,
                ct
            );
        }

        return new NicotvOpenResponse(request.FurnitureId, nicotvId, NicotvMapper.ToPacket(nicotv));
    }
}
