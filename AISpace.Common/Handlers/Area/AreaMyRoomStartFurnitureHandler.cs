using AISpace.Common.DAL.Repositories;
using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Packets.Area;
using Microsoft.Extensions.Logging;

namespace AISpace.Common.Handlers.Area;

public class AreaMyRoomStartFurnitureHandler(
    IMyRoomRepository myRoomRepository,
    ILogger<AreaMyRoomStartFurnitureHandler> logger
) : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.MyRoomStartFurnitureRequest;

    public PacketType ResponseType => PacketType.MyRoomStartFurnitureResponse;

    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var request = MyRoomStartFurnitureRequest.FromBytes(payload.Span);

        if (
            !await MyRoomRequestValidation.IsOwnerInRoomAsync(
                request.RoomId,
                session,
                myRoomRepository,
                ct
            )
        )
        {
            logger.LogWarning(
                "Rejected MyRoomStartFurniture for character {CharacterId} on map {MapId}: roomId {RoomId}",
                session.CharacterId,
                session.MapId,
                request.RoomId
            );
            await session.SendAsync(
                ResponseType,
                new MyRoomStartFurnitureResponse(1, 0).ToBytes(),
                ct
            );
            return;
        }

        var stage = MyRoomInfo.GetRoomStage(session.MapId);
        var maxPlacement = MyRoomInfo.GetMaxFurniturePlacement(stage);
        session.PendingMyRoomFurnitureItemId = null;

        logger.LogInformation(
            "MyRoomStartFurniture for character {CharacterId} on map {MapId} (stage {Stage}, max {MaxPlacement})",
            session.CharacterId,
            session.MapId,
            stage,
            maxPlacement
        );

        await session.SendAsync(
            ResponseType,
            new MyRoomStartFurnitureResponse(0, maxPlacement).ToBytes(),
            ct
        );
    }
}
