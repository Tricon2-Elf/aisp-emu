using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Data;
using aisp.Network.Packets.Area;

namespace aisp.Common.Handlers.Area;

public sealed class AreaNicotvPlayHandler(
    INicotvRepository nicotvRepository,
    SharedState state,
    ScreenAssignments screenAssignments
) : PacketHandlerBase<NicotvPlayRequest, NicotvPlayResponse>, IRequiresAuthenticatedSession
{
    public override PacketType RequestType => PacketType.NicotvPlayRequest;
    public override PacketType ResponseType => PacketType.NicotvPlayResponse;
    public override ServerType ServerType => ServerType.Area;

    public override async Task<NicotvPlayResponse?> HandleAsync(
        NicotvPlayRequest request,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        if (
            !MyRoomInfo.IsMyRoomMap(session.MapId)
            || session.MyRoomId == 0
            || session.MyRoomId > int.MaxValue
            || !Enum.IsDefined(typeof(NicotvPlaybackState), request.Status)
        )
            return new NicotvPlayResponse(1, request.NicotvId);

        var nicotv = await nicotvRepository.SetPlaybackStateAsync(
            checked((int)session.MyRoomId),
            request.NicotvId,
            (NicotvPlaybackState)request.Status,
            ct
        );
        if (nicotv is null)
            return new NicotvPlayResponse(1, request.NicotvId);

        // The pause button: moves that TV's shared timeline for everyone in the room with it.
        // n: keys the timeline by this specific TV, same as AreaNicotvSetMovieHandler.
        var status = (NicotvPlaybackState)request.Status;
        if (status is NicotvPlaybackState.Paused or NicotvPlaybackState.Playing)
            screenAssignments.ControlMovie(
                session.MapId,
                session.ChannelId,
                NicotvMapper.WithNicotvId(nicotv.MovieId, request.NicotvId),
                status == NicotvPlaybackState.Paused ? "pause" : "resume"
            );

        // The client calls ext_play on its own screen browser for this notify regardless of
        // Status (confirmed by testing both values), so Status goes out as-is rather than as a
        // play/pause toggle; the shared timeline (moved above) and the page's poll of it are
        // what pause and resume actually run on. includeSource lets the sender see its own copy
        // too, same as AreaNicotvSetMovieHandler.
        await MyRoomFurnitureNotification.BroadcastToRoomAsync(
            state,
            session,
            session.MyRoomId,
            PacketType.NotifyNicotvPlay,
            new NotifyNicotvPlay(request.NicotvId, request.Status).ToBytes(),
            includeSource: true,
            ct
        );
        return new NicotvPlayResponse(0, request.NicotvId);
    }
}
