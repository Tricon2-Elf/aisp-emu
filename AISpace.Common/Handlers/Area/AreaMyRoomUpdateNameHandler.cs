using AISpace.Common.DAL.Repositories;
using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Packets.Area;

namespace AISpace.Common.Handlers.Area;

public sealed class AreaMyRoomUpdateNameHandler(IMyRoomRepository myRoomRepository)
    : PacketHandlerBase<MyRoomUpdateNameRequest, MyRoomUpdateNameResponse>,
        IRequiresAuthenticatedSession
{
    public override PacketType RequestType => PacketType.MyRoomUpdateNameRequest;
    public override PacketType ResponseType => PacketType.MyRoomUpdateNameResponse;
    public override ServerType ServerType => ServerType.Area;

    public override async Task<MyRoomUpdateNameResponse?> HandleAsync(
        MyRoomUpdateNameRequest request,
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
            return new MyRoomUpdateNameResponse(1);

        var updated = await myRoomRepository.UpdateNameAsync(
            checked((int)request.RoomId),
            checked((int)session.CharacterId),
            request.Name,
            ct
        );

        return new MyRoomUpdateNameResponse(updated ? 0u : 1u);
    }
}
