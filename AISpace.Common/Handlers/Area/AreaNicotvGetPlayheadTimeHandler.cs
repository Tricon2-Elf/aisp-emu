using AISpace.Common.DAL.Repositories;
using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Packets.Area;

namespace AISpace.Common.Handlers.Area;

public sealed class AreaNicotvGetPlayheadTimeHandler(
    INicotvRepository nicotvRepository,
    SharedState state
)
    : PacketHandlerBase<NicotvGetPlayheadTimeRequest, NicotvGetPlayheadTimeResponse>,
        IRequiresAuthenticatedSession
{
    public override PacketType RequestType => PacketType.NicotvGetPlayheadTimeRequest;
    public override PacketType ResponseType => PacketType.NicotvGetPlayheadTimeResponse;
    public override ServerType ServerType => ServerType.Area;

    public override async Task<NicotvGetPlayheadTimeResponse?> HandleAsync(
        NicotvGetPlayheadTimeRequest request,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        if (
            !MyRoomInfo.IsMyRoomMap(session.MapId)
            || session.MyRoomId == 0
            || session.MyRoomId > int.MaxValue
        )
            return new NicotvGetPlayheadTimeResponse(request.NicotvId, 0);

        var nicotv = await nicotvRepository.GetByIdInRoomAsync(
            checked((int)session.MyRoomId),
            request.NicotvId,
            ct
        );
        if (nicotv is null)
            return new NicotvGetPlayheadTimeResponse(request.NicotvId, 0);

        var requestingUserId = session.User?.Id ?? session.UserId;
        var peer = state.GetAreaPeers(session).FirstOrDefault();
        if (requestingUserId <= 0 || peer is null)
            return new NicotvGetPlayheadTimeResponse(request.NicotvId, 0);

        await peer.SendAsync(
            PacketType.NicotvGetPlayheadTimeRequestNotify,
            new NicotvGetPlayheadTimeRequestNotify(
                request.NicotvId,
                checked((uint)requestingUserId)
            ).ToBytes(),
            ct
        );
        return null;
    }
}
