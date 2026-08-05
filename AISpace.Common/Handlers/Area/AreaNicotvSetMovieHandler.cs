using AISpace.Common.DAL.Repositories;
using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Packets.Area;

namespace AISpace.Common.Handlers.Area;

public sealed class AreaNicotvSetMovieHandler(INicotvRepository nicotvRepository, SharedState state)
    : PacketHandlerBase<NicotvSetMovieRequest, NicotvSetMovieResponse>,
        IRequiresAuthenticatedSession
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

        await MyRoomFurnitureNotification.BroadcastToRoomAsync(
            state,
            session,
            session.MyRoomId,
            PacketType.NotifyNicotvSetMovie,
            new NotifyNicotvSetMovie(request.NicotvId, request.MovieId).ToBytes(),
            includeSource: false,
            ct
        );
        return new NicotvSetMovieResponse(0, request.NicotvId);
    }
}
