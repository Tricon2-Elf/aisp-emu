using aisp.Common.DAL.Entities;
using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Data;
using aisp.Network.Packets.Area;
using Microsoft.Extensions.Logging;

namespace aisp.Common.Handlers.Area;

public sealed class AreaMyRoomSetFurnitureHandler(
    IMyRoomRepository myRoomRepository,
    SharedState state,
    ILogger<AreaMyRoomSetFurnitureHandler> logger
) : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.MyRoomSetFurnitureRequest;
    public PacketType ResponseType => PacketType.MyRoomSetFurnitureResponse;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var request = MyRoomSetFurnitureRequest.FromBytes(payload.Span);
        if (
            !await MyRoomRequestValidation.IsOwnerInRoomAsync(
                request.RoomId,
                session,
                myRoomRepository,
                ct
            )
        )
        {
            session.PendingMyRoomFurnitureItemId = null;
            await session.SendAsync(ResponseType, new MyRoomSetFurnitureResponse(1).ToBytes(), ct);
            return;
        }

        if (request.SerialId > int.MaxValue)
        {
            session.PendingMyRoomFurnitureItemId = null;
            await session.SendAsync(ResponseType, new MyRoomSetFurnitureResponse(1).ToBytes(), ct);
            return;
        }

        var placementLimit = MyRoomInfo.GetMaxFurniturePlacement(
            MyRoomInfo.GetRoomStage(session.MapId)
        );
        var characterId = checked((int)session.CharacterId);
        var itemId = checked((int)request.SerialId);

        // The client always sends an all-zero request immediately after it
        // creates the hidden -256 preview object. A successful response moves
        // the client from state 501 to 502 so it can position that preview.
        // This is validation only: assigning a furniture ID here causes the
        // eventual server notification to remove the preview.
        if (request.Transform == default)
        {
            var canPlace = await myRoomRepository.CanPlaceFurnitureAsync(
                characterId,
                checked((int)request.RoomId),
                itemId,
                placementLimit,
                ct
            );
            session.PendingMyRoomFurnitureItemId = canPlace ? request.SerialId : null;
            await session.SendAsync(
                ResponseType,
                new MyRoomSetFurnitureResponse(canPlace ? 0u : 1u).ToBytes(),
                ct
            );
            logger.LogInformation(
                "MyRoom furniture preview {Result} for character {CharacterId}, item {ItemId}",
                canPlace ? "accepted" : "rejected",
                session.CharacterId,
                request.SerialId
            );
            return;
        }

        if (session.PendingMyRoomFurnitureItemId != request.SerialId)
        {
            session.PendingMyRoomFurnitureItemId = null;
            await session.SendAsync(ResponseType, new MyRoomSetFurnitureResponse(1).ToBytes(), ct);
            logger.LogWarning(
                "Rejected MyRoom furniture commit for character {CharacterId}, item {ItemId}: no matching preview reservation",
                session.CharacterId,
                request.SerialId
            );
            return;
        }

        session.PendingMyRoomFurnitureItemId = null;
        var furniture = await myRoomRepository.TryAddFurnitureAsync(
            characterId,
            new MyRoomFurniture
            {
                RoomId = checked((int)request.RoomId),
                ItemId = itemId,
                PositionX = request.Transform.X,
                PositionY = request.Transform.Y,
                PositionZ = request.Transform.Z,
                DirectionX = request.Transform.DirectionX,
                DirectionY = request.Transform.DirectionY,
            },
            placementLimit,
            ct
        );

        await session.SendAsync(
            ResponseType,
            new MyRoomSetFurnitureResponse(furniture is null ? 1u : 0u).ToBytes(),
            ct
        );
        if (furniture is not null)
        {
            await MyRoomFurnitureNotification.BroadcastToRoomAsync(
                state,
                session,
                request.RoomId,
                PacketType.NotifyMyRoomSetFurniture,
                new NotifyMyRoomSetFurniture(MyRoomFurnitureMapper.ToPacket(furniture)).ToBytes(),
                includeSource: true,
                ct
            );
            var inventory = await myRoomRepository.GetAvailableFurnitureInventoryAsync(
                characterId,
                ct
            );
            await CharacterItemSync.SendFurnitureInventoryAvailabilityAsync(
                session,
                itemId,
                inventory.GetValueOrDefault(itemId),
                ct
            );
            logger.LogInformation(
                "Committed MyRoom furniture {FurnitureId} for character {CharacterId}, item {ItemId} at ({X}, {Y}, {Z})",
                furniture.FurnitureId,
                session.CharacterId,
                request.SerialId,
                request.Transform.X,
                request.Transform.Y,
                request.Transform.Z
            );
        }
    }
}
