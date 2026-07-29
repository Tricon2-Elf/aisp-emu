using AISpace.Common.DAL.Repositories;
using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Packets.Area;

namespace AISpace.Common.Handlers.Area;

public sealed class AreaMyRoomUpdateSecurityHandler(IMyRoomRepository myRoomRepository)
    : PacketHandlerBase<MyRoomUpdateSecurityRequest, MyRoomUpdateSecurityResponse>,
        IRequiresAuthenticatedSession
{
    public override PacketType RequestType => PacketType.MyRoomUpdateSecurityRequest;
    public override PacketType ResponseType => PacketType.MyRoomUpdateSecurityResponse;
    public override ServerType ServerType => ServerType.Area;

    public override async Task<MyRoomUpdateSecurityResponse?> HandleAsync(
        MyRoomUpdateSecurityRequest request,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        if (
            !await MyRoomRequestValidation.IsOwnerInRoomAsync(
                request.RoomId,
                session,
                myRoomRepository,
                ct
            )
        )
            return new MyRoomUpdateSecurityResponse(1);

        var updated = await myRoomRepository.UpdateSecurityAsync(
            checked((int)request.RoomId),
            checked((int)session.CharacterId),
            request.Security,
            ct
        );

        return new MyRoomUpdateSecurityResponse(updated ? 0u : 1u);
    }
}
