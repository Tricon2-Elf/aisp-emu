using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Data;
using aisp.Network.Packets.Msg;

namespace aisp.Common.Handlers.Msg;

public class CircleGetDataHandler(ICircleRepository circles, SharedState state)
    : IPacketHandler,
        IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.CircleGetDataRequest;
    public PacketType ResponseType => PacketType.CircleGetDataResponse;
    public ServerType ServerType => ServerType.Msg;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        _ = CircleGetDataRequest.FromBytes(payload.Span);
        var memberships = await circles.GetMembershipsForCharacterAsync(
            (int)session.CharacterId,
            ct
        );
        (CircleData, uint)[] list =
        [
            .. memberships.Select(m => (circles.ToCircleData(m.Circle), m.AuthLevel)),
        ];

        await session.SendAsync(ResponseType, new CircleGetDataResponse(0, list).ToBytes(), ct);

        foreach (var (circle, _) in memberships)
            await CircleNotifyHelper.SendRosterAsync(circles, state, circle.Id, ct);
    }
}
