using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Packets.Area;

namespace aisp.Common.Handlers.Area;

public sealed class AreaMyRoomUpdateNameHandler(
    IMyRoomRepository myRoomRepository,
    IWordFilter wordFilter
)
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

        if (wordFilter.ContainsBlockedWord(WordFilterLevel.Complete, request.Name))
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
