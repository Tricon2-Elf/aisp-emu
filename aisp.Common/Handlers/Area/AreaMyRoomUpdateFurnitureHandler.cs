using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Packets.Area;

namespace aisp.Common.Handlers.Area;

public sealed class AreaMyRoomUpdateFurnitureHandler(
    IMyRoomRepository myRoomRepository,
    SharedState state
) : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.MyRoomUpdateFurnitureRequest;
    public PacketType ResponseType => PacketType.MyRoomUpdateFurnitureResponse;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var request = MyRoomUpdateFurnitureRequest.FromBytes(payload.Span);
        if (
            !await MyRoomRequestValidation.IsOwnerInRoomAsync(
                request.RoomId,
                session,
                myRoomRepository,
                ct
            )
        )
        {
            await session.SendAsync(
                ResponseType,
                new MyRoomUpdateFurnitureResponse(1).ToBytes(),
                ct
            );
            return;
        }

        var transform = request.Transform;
        var updated = await myRoomRepository.UpdateFurnitureAsync(
            checked((int)request.RoomId),
            request.FurnitureId,
            transform.X,
            transform.Y,
            transform.Z,
            transform.DirectionX,
            transform.DirectionY,
            ct
        );
        await session.SendAsync(
            ResponseType,
            new MyRoomUpdateFurnitureResponse(updated ? 0u : 1u).ToBytes(),
            ct
        );
        if (updated)
            await MyRoomFurnitureNotification.BroadcastToRoomAsync(
                state,
                session,
                request.RoomId,
                PacketType.NotifyMyRoomUpdateFurniture,
                new NotifyMyRoomUpdateFurniture(
                    request.RoomId,
                    request.FurnitureId,
                    transform
                ).ToBytes(),
                includeSource: false,
                ct
            );
    }
}
