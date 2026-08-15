using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Data;
using aisp.Network.Packets.Area;

namespace aisp.Common.Handlers.Area;

public sealed class AreaNicotvPlayHandler(INicotvRepository nicotvRepository, SharedState state)
    : PacketHandlerBase<NicotvPlayRequest, NicotvPlayResponse>,
        IRequiresAuthenticatedSession
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

        await MyRoomFurnitureNotification.BroadcastToRoomAsync(
            state,
            session,
            session.MyRoomId,
            PacketType.NotifyNicotvPlay,
            new NotifyNicotvPlay(request.NicotvId, request.Status).ToBytes(),
            includeSource: false,
            ct
        );
        return new NicotvPlayResponse(0, request.NicotvId);
    }
}
