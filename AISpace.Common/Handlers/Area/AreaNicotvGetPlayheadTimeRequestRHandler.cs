using AISpace.Common.DAL.Repositories;
using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Packets.Area;

namespace AISpace.Common.Handlers.Area;

public sealed class AreaNicotvGetPlayheadTimeRequestRHandler(
    INicotvRepository nicotvRepository,
    SharedState state
) : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.NicotvGetPlayheadTimeRequestRRequest;
    public PacketType ResponseType => (PacketType)0;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var request = NicotvGetPlayheadTimeRequestRRequest.FromBytes(payload.Span);
        if (
            !MyRoomInfo.IsMyRoomMap(session.MapId)
            || session.MyRoomId == 0
            || session.MyRoomId > int.MaxValue
            || request.RequestingUserId == 0
            || request.RequestingUserId > int.MaxValue
        )
            return;

        var nicotv = await nicotvRepository.GetByIdInRoomAsync(
            checked((int)session.MyRoomId),
            request.NicotvId,
            ct
        );
        if (nicotv is null)
            return;

        var requester = state.GetAreaSessionByUserId(
            checked((int)request.RequestingUserId),
            session.MapId,
            session.ChannelId
        );
        if (requester is null || requester.MyRoomId != session.MyRoomId)
            return;

        await requester.SendAsync(
            PacketType.NicotvGetPlayheadTimeResponse,
            new NicotvGetPlayheadTimeResponse(request.NicotvId, request.Seconds).ToBytes(),
            ct
        );
    }
}
