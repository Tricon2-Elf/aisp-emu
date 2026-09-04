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

        var nicotv = await nicotvRepository.UpdateForFurnitureAsync(
            roomId,
            request.FurnitureId,
            request.Nicotv,
            ct
        );
        if (nicotv is null)
            return new NicotvOpenResponse(request.FurnitureId, 0, new NicotvData());

        var nicotvId = checked((uint)nicotv.Id);

        // Open carries the client's NicotvData snapshot, so peers get notifies for the fields that
        // changed. Not comment visibility: the TV panel builds this snapshot from constants
        // (playing, comments visible) whatever the TV shows, and the client has no request for
        // that field at all (its panel button is an empty case in the binary). The stored value
        // is the server's own default, which reaches the page in its title and is never read
        // back from here; only NotifyNicotvSetCommentVisible can change it on a running client.
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

        return new NicotvOpenResponse(request.FurnitureId, nicotvId, NicotvMapper.ToPacket(nicotv));
    }
}
