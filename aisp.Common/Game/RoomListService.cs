using aisp.Common.DAL.Entities;
using aisp.Common.DAL.Repositories;
using aisp.Network;
using aisp.Network.Data;
using aisp.Network.Packets.Area;
using Microsoft.Extensions.Logging;

namespace aisp.Common.Game;

/// <summary>
/// Opens the client "誰の部屋にいくの？" room list (OpenStart → Pack → OpenEnd).
/// </summary>
public sealed class RoomListService(
    IMyRoomRepository myRoomRepository,
    ICircleRepository circleRepository,
    SharedState state,
    ILogger<RoomListService> logger
)
{
    private const int MaxListedRooms = 100;

    public async Task OpenAsync(IPlayerSession session, CancellationToken ct = default)
    {
        var visitorId = checked((int)session.CharacterId);
        var rooms = await myRoomRepository.GetCandidateVisitRoomsAsync(
            visitorId,
            MaxListedRooms,
            ct
        );

        var entries = new List<RoomListEntry>(rooms.Count);
        foreach (var room in rooms)
        {
            var sharesCircle = await circleRepository.SharesAnyCircleAsync(
                visitorId,
                room.OwnerCharacterId,
                ct
            );
            if (!MyRoomAccess.CanEnter(room, visitorId, sharesCircle))
                continue;

            entries.Add(
                new RoomListEntry(
                    (uint)room.Id,
                    room.Name,
                    room.OwnerCharacter?.Name ?? string.Empty,
                    ResolveStatus(room)
                )
            );
        }

        await session.SendAsync(
            PacketType.NotifyRoomListOpenStart,
            new NotifyRoomListOpenStart().ToBytes(),
            ct
        );

        for (var offset = 0; offset < entries.Count; offset += NotifyRoomListPack.MaximumRooms)
        {
            var chunk = entries.Skip(offset).Take(NotifyRoomListPack.MaximumRooms).ToArray();
            await session.SendAsync(
                PacketType.NotifyRoomListPack,
                new NotifyRoomListPack(chunk).ToBytes(),
                ct
            );
        }

        await session.SendAsync(
            PacketType.NotifyRoomListOpenEnd,
            new NotifyRoomListOpenEnd().ToBytes(),
            ct
        );

        logger.LogInformation(
            "Opened room list for character {CharacterId} with {Count} entries: {Statuses}",
            session.CharacterId,
            entries.Count,
            string.Join(", ", entries.Select(e => $"{e.RoomId}={e.Status}"))
        );
    }

    private uint ResolveStatus(Room room)
    {
        var ownerOnline = false;
        foreach (var areaSession in state.GetServerClients(ServerType.Area))
        {
            if (
                !areaSession.IsAuthenticated
                || areaSession.CharacterId != (uint)room.OwnerCharacterId
            )
                continue;

            ownerOnline = true;
            if (
                MyRoomInfo.IsMyRoomMap(areaSession.MapId)
                && areaSession.MyRoomId == (uint)room.Id
            )
                return RoomListStatus.AtHome;

            if (MyRoomInfo.IsMyRoomMap(areaSession.MapId))
                return RoomListStatus.Away;
        }

        // Msg-only (e.g. character select) still counts as online-but-out for the list.
        if (!ownerOnline)
        {
            foreach (var msgSession in state.GetServerClients(ServerType.Msg))
            {
                if (
                    msgSession.IsAuthenticated
                    && msgSession.CharacterId == (uint)room.OwnerCharacterId
                )
                {
                    ownerOnline = true;
                    break;
                }
            }
        }

        return ownerOnline ? RoomListStatus.Out : RoomListStatus.LoggedOut;
    }
}
