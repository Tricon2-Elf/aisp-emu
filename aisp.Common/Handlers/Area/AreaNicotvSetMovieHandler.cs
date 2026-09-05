using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Packets.Area;

namespace aisp.Common.Handlers.Area;

public sealed class AreaNicotvSetMovieHandler(
    INicotvRepository nicotvRepository,
    SharedState state,
    ScreenAssignments screenAssignments
) : PacketHandlerBase<NicotvSetMovieRequest, NicotvSetMovieResponse>, IRequiresAuthenticatedSession
{
    public override PacketType RequestType => PacketType.NicotvSetMovieRequest;
    public override PacketType ResponseType => PacketType.NicotvSetMovieResponse;
    public override ServerType ServerType => ServerType.Area;

    public override async Task<NicotvSetMovieResponse?> HandleAsync(
        NicotvSetMovieRequest request,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        if (
            !MyRoomInfo.IsMyRoomMap(session.MapId)
            || session.MyRoomId == 0
            || session.MyRoomId > int.MaxValue
        )
            return new NicotvSetMovieResponse(1, request.NicotvId);

        var nicotv = await nicotvRepository.SetMovieAsync(
            checked((int)session.MyRoomId),
            request.NicotvId,
            request.MovieId,
            ct
        );
        if (nicotv is null)
            return new NicotvSetMovieResponse(1, request.NicotvId);

        // The n: tag rides along on the room TV's own movie id: the screen page
        // round-trips it into its movieid= URL, letting the server key the shared timeline (and,
        // on the next poll, resolve content) by this specific TV rather than by map and channel,
        // which do not reliably tell one player's room from another's.
        var taggedMovieId = NicotvMapper.WithNicotvId(request.MovieId, request.NicotvId);

        // A movie picked on the TV plays from the start for the room, even if that same id is
        // already mid-playback somewhere else.
        screenAssignments.SetMovie(session.MapId, session.ChannelId, taggedMovieId);

        await MyRoomFurnitureNotification.BroadcastToRoomAsync(
            state,
            session,
            session.MyRoomId,
            PacketType.NotifyNicotvSetMovie,
            new NotifyNicotvSetMovie(request.NicotvId, taggedMovieId).ToBytes(),
            // The client ignores set_movie_r (an 8-byte no-op, verified); only the notify makes
            // the sender's own TV load the movie, so it must get it too.
            includeSource: true,
            ct
        );
        return new NicotvSetMovieResponse(0, request.NicotvId);
    }
}
