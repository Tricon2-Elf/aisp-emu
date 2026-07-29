using AISpace.Common.DAL.Entities;
using AISpace.Common.DAL.Repositories;
using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Packets.Area;
using Microsoft.Extensions.Logging;

namespace AISpace.Common.Handlers.Area;

public sealed class AreaMyRoomUpdateNameHandler(IMyRoomRepository myRoomRepository) : PacketHandlerBase<MyRoomUpdateNameRequest, MyRoomUpdateNameResponse>, IRequiresAuthenticatedSession
{
    public override PacketType RequestType => PacketType.MyRoomUpdateNameRequest;
    public override PacketType ResponseType => PacketType.MyRoomUpdateNameResponse;
    public override ServerType ServerType => ServerType.Area;

    public override async Task<MyRoomUpdateNameResponse?> HandleAsync(MyRoomUpdateNameRequest request, IPlayerSession session, CancellationToken ct = default)
    {
        if (!MyRoomRequestValidation.IsOwnerInRoom(request.RoomId, session))
            return new MyRoomUpdateNameResponse(1);

        var updated = await myRoomRepository.UpdateNameAsync(checked((int)session.CharacterId), request.Name, ct);
        if (updated && session.Character is not null)
            session.Character.MyRoomName = request.Name;

        return new MyRoomUpdateNameResponse(updated ? 0u : 1u);
    }
}

public sealed class AreaMyRoomUpdateSecurityHandler(IMyRoomRepository myRoomRepository) : PacketHandlerBase<MyRoomUpdateSecurityRequest, MyRoomUpdateSecurityResponse>, IRequiresAuthenticatedSession
{
    public override PacketType RequestType => PacketType.MyRoomUpdateSecurityRequest;
    public override PacketType ResponseType => PacketType.MyRoomUpdateSecurityResponse;
    public override ServerType ServerType => ServerType.Area;

    public override async Task<MyRoomUpdateSecurityResponse?> HandleAsync(MyRoomUpdateSecurityRequest request, IPlayerSession session, CancellationToken ct = default)
    {
        if (!MyRoomRequestValidation.IsOwnerInRoom(request.RoomId, session))
            return new MyRoomUpdateSecurityResponse(1);

        var updated = await myRoomRepository.UpdateSecurityAsync(checked((int)session.CharacterId), request.Security, ct);
        if (updated && session.Character is not null)
            session.Character.MyRoomSecurity = request.Security;

        return new MyRoomUpdateSecurityResponse(updated ? 0u : 1u);
    }
}

public sealed class AreaMyRoomSetFurnitureHandler(IMyRoomRepository myRoomRepository, SharedState state, ILogger<AreaMyRoomSetFurnitureHandler> logger) : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.MyRoomSetFurnitureRequest;
    public PacketType ResponseType => PacketType.MyRoomSetFurnitureResponse;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, IPlayerSession session, CancellationToken ct = default)
    {
        var request = MyRoomSetFurnitureRequest.FromBytes(payload.Span);
        if (!MyRoomRequestValidation.IsOwnerInRoom(request.RoomId, session))
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

        var placementLimit = MyRoomInfo.GetMaxFurniturePlacement(MyRoomInfo.GetRoomStage(session.MapId));
        var characterId = checked((int)session.CharacterId);
        var itemId = checked((int)request.SerialId);

        // The client always sends an all-zero request immediately after it
        // creates the hidden -256 preview object. A successful response moves
        // the client from state 501 to 502 so it can position that preview.
        // This is validation only: assigning a furniture ID here causes the
        // eventual server notification to remove the preview.
        if (request.Transform == default)
        {
            var canPlace = await myRoomRepository.CanPlaceFurnitureAsync(characterId, itemId, placementLimit, ct);
            session.PendingMyRoomFurnitureItemId = canPlace ? request.SerialId : null;
            await session.SendAsync(ResponseType, new MyRoomSetFurnitureResponse(canPlace ? 0u : 1u).ToBytes(), ct);
            logger.LogInformation("MyRoom furniture preview {Result} for character {CharacterId}, item {ItemId}", canPlace ? "accepted" : "rejected", session.CharacterId, request.SerialId);
            return;
        }

        if (session.PendingMyRoomFurnitureItemId != request.SerialId)
        {
            session.PendingMyRoomFurnitureItemId = null;
            await session.SendAsync(ResponseType, new MyRoomSetFurnitureResponse(1).ToBytes(), ct);
            logger.LogWarning("Rejected MyRoom furniture commit for character {CharacterId}, item {ItemId}: no matching preview reservation", session.CharacterId, request.SerialId);
            return;
        }

        session.PendingMyRoomFurnitureItemId = null;
        var furniture = await myRoomRepository.TryAddFurnitureAsync(
            new MyRoomFurniture
            {
                CharacterId = characterId,
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

        await session.SendAsync(ResponseType, new MyRoomSetFurnitureResponse(furniture is null ? 1u : 0u).ToBytes(), ct);
        if (furniture is not null)
        {
            await MyRoomFurnitureNotification.BroadcastToRoomAsync(state, session, request.RoomId, PacketType.NotifyMyRoomSetFurniture, new NotifyMyRoomSetFurniture(MyRoomFurnitureMapper.ToPacket(furniture)).ToBytes(), includeSource: true, ct);
            logger.LogInformation("Committed MyRoom furniture {FurnitureId} for character {CharacterId}, item {ItemId} at ({X}, {Y}, {Z})", furniture.FurnitureId, session.CharacterId, request.SerialId, request.Transform.X, request.Transform.Y, request.Transform.Z);
        }
    }
}

public sealed class AreaMyRoomRemoveFurnitureHandler(IMyRoomRepository myRoomRepository, SharedState state) : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.MyRoomRemoveFurnitureRequest;
    public PacketType ResponseType => PacketType.MyRoomRemoveFurnitureResponse;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, IPlayerSession session, CancellationToken ct = default)
    {
        var request = MyRoomRemoveFurnitureRequest.FromBytes(payload.Span);
        if (!MyRoomRequestValidation.IsOwnerInRoom(request.RoomId, session))
        {
            await session.SendAsync(ResponseType, new MyRoomRemoveFurnitureResponse(1).ToBytes(), ct);
            return;
        }

        var removed = await myRoomRepository.RemoveFurnitureAsync(checked((int)session.CharacterId), request.FurnitureId, ct);
        await session.SendAsync(ResponseType, new MyRoomRemoveFurnitureResponse(removed is null ? 1u : 0u).ToBytes(), ct);
        if (removed is not null)
            await MyRoomFurnitureNotification.BroadcastToRoomAsync(state, session, request.RoomId, PacketType.NotifyMyRoomRemoveFurniture, new NotifyMyRoomRemoveFurniture(request.RoomId, request.FurnitureId).ToBytes(), includeSource: false, ct);
    }
}

public sealed class AreaMyRoomUpdateFurnitureHandler(IMyRoomRepository myRoomRepository, SharedState state) : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.MyRoomUpdateFurnitureRequest;
    public PacketType ResponseType => PacketType.MyRoomUpdateFurnitureResponse;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, IPlayerSession session, CancellationToken ct = default)
    {
        var request = MyRoomUpdateFurnitureRequest.FromBytes(payload.Span);
        if (!MyRoomRequestValidation.IsOwnerInRoom(request.RoomId, session))
        {
            await session.SendAsync(ResponseType, new MyRoomUpdateFurnitureResponse(1).ToBytes(), ct);
            return;
        }

        var transform = request.Transform;
        var updated = await myRoomRepository.UpdateFurnitureAsync(checked((int)session.CharacterId), request.FurnitureId, transform.X, transform.Y, transform.Z, transform.DirectionX, transform.DirectionY, ct);
        await session.SendAsync(ResponseType, new MyRoomUpdateFurnitureResponse(updated ? 0u : 1u).ToBytes(), ct);
        if (updated)
            await MyRoomFurnitureNotification.BroadcastToRoomAsync(state, session, request.RoomId, PacketType.NotifyMyRoomUpdateFurniture, new NotifyMyRoomUpdateFurniture(request.RoomId, request.FurnitureId, transform).ToBytes(), includeSource: false, ct);
    }
}

internal static class MyRoomRequestValidation
{
    public static bool IsOwnerInRoom(uint roomId, IPlayerSession session) => session.CharacterId != 0 && roomId == session.CharacterId && roomId == session.MyRoomOwnerId && MyRoomInfo.IsMyRoomMap(session.MapId);
}

internal static class MyRoomFurnitureMapper
{
    public static MyRoomFurnitureData ToPacket(MyRoomFurniture furniture) => new(checked((uint)furniture.CharacterId), furniture.FurnitureId, PlacementState: 0, checked((uint)furniture.ItemId), furniture.PositionX, furniture.PositionY, furniture.PositionZ, furniture.DirectionX, furniture.DirectionY, Active: 1);
}

internal static class MyRoomFurnitureNotification
{
    public static async Task BroadcastToRoomAsync(SharedState state, IPlayerSession source, uint roomId, PacketType packetType, byte[] payload, bool includeSource, CancellationToken ct)
    {
        var recipients = state.GetAreaPeers(source, includeSource).Where(peer => peer.MyRoomOwnerId == roomId);
        foreach (var peer in recipients)
            await peer.SendAsync(packetType, payload, ct);
    }
}
