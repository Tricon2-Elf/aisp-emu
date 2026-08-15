using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Packets.Area;

namespace aisp.Common.Handlers.Area;

public sealed class AreaMyRoomRemoveFurnitureHandler(
    IMyRoomRepository myRoomRepository,
    SharedState state
) : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.MyRoomRemoveFurnitureRequest;
    public PacketType ResponseType => PacketType.MyRoomRemoveFurnitureResponse;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var request = MyRoomRemoveFurnitureRequest.FromBytes(payload.Span);
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
                new MyRoomRemoveFurnitureResponse(1).ToBytes(),
                ct
            );
            return;
        }

        var removed = await myRoomRepository.RemoveFurnitureAsync(
            checked((int)request.RoomId),
            request.FurnitureId,
            ct
        );
        await session.SendAsync(
            ResponseType,
            new MyRoomRemoveFurnitureResponse(removed is null ? 1u : 0u).ToBytes(),
            ct
        );
        if (removed is not null)
        {
            await MyRoomFurnitureNotification.BroadcastToRoomAsync(
                state,
                session,
                request.RoomId,
                PacketType.NotifyMyRoomRemoveFurniture,
                new NotifyMyRoomRemoveFurniture(request.RoomId, request.FurnitureId).ToBytes(),
                includeSource: false,
                ct
            );
            var inventory = await myRoomRepository.GetAvailableFurnitureInventoryAsync(
                checked((int)session.CharacterId),
                ct
            );
            await CharacterItemSync.SendFurnitureInventoryAvailabilityAsync(
                session,
                removed.ItemId,
                inventory.GetValueOrDefault(removed.ItemId),
                ct
            );
        }
    }
}
