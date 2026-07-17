using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Packets.Area;
using Microsoft.Extensions.Logging;

namespace AISpace.Common.Handlers.Area;

/// <summary>
/// Handles send_myroom_use_furniture. Builtin closet (serial 2) opens account storage via
/// recv_storage_opened; other furnids are acknowledged only for now.
/// </summary>
public class AreaMyRoomUseFurnitureHandler(ILogger<AreaMyRoomUseFurnitureHandler> logger) : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.MyRoomUseFurnitureRequest;

    public PacketType ResponseType => PacketType.MyRoomUseFurnitureResponse;

    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, IPlayerSession session, CancellationToken ct = default)
    {
        var request = MyRoomUseFurnitureRequest.FromBytes(payload.Span);

        if (!MyRoomInfo.IsMyRoomMap(session.MapId) || request.RoomId != session.CharacterId)
        {
            logger.LogWarning(
                "Rejected MyRoomUseFurniture for character {CharacterId} on map {MapId}: roomId {RoomId} furnId {FurnId}",
                session.CharacterId,
                session.MapId,
                request.RoomId,
                request.FurnId
            );
            await session.SendAsync(ResponseType, new MyRoomUseFurnitureResponse(1).ToBytes(), ct);
            return;
        }

        await session.SendAsync(ResponseType, new MyRoomUseFurnitureResponse(0).ToBytes(), ct);

        if (request.FurnId == MyRoomInfo.ClosetSerialId)
        {
            // Client opens 倉庫 on recv_storage_opened (aipoint balance). Deposit/withdraw not wired yet.
            await session.SendAsync(PacketType.StorageOpenedNotify, new StorageOpenedNotify(0).ToBytes(), ct);
            logger.LogInformation(
                "Opened storage for character {CharacterId} via MyRoom closet (furnId {FurnId})",
                session.CharacterId,
                request.FurnId
            );
        }
        else
        {
            logger.LogInformation(
                "MyRoomUseFurniture ack for character {CharacterId} furnId {FurnId} reason {Reason} (no storage open)",
                session.CharacterId,
                request.FurnId,
                request.Reason
            );
        }
    }
}
