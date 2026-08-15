using aisp.Common.DAL.Repositories;
using aisp.Common.Handlers.Area;
using aisp.Network;
using aisp.Network.Data;
using aisp.Network.Packets.Area;
using Microsoft.Extensions.Logging;

namespace aisp.Common.Game;

/// <summary>
/// Spawns the entering player for area peers and existing peers for the entering player.
/// Deferred until MapEnter so the client finishes map load before remote avatars arrive.
/// </summary>
public static class AreaAvatarPresenceSync
{
    public static async Task SynchronizePeersAsync(
        SharedState state,
        IPlayerSession session,
        ILogger logger,
        IRoboRepository? roboRepository = null,
        IMyRoomRepository? myRoomRepository = null,
        CancellationToken ct = default
    )
    {
        var myChar = session.Character ?? session.User?.Characters.FirstOrDefault();
        if (myChar is null)
            return;

        var myPos = new MovementData(
            session.X,
            session.Y,
            session.Z,
            session.Rotation,
            MovementType.Stopped
        );
        IReadOnlyList<RoboData> accompanyingRobos = [];
        if (roboRepository is not null)
            accompanyingRobos = (
                await roboRepository.GetAllAsync(checked((int)session.CharacterId), ct)
            )
                .Where(x => session.AccompanyingRoboIds.Contains(x.RoboId))
                .ToList();

        var spawnMeForPeersPacket = AreasvEnterHandler.CreateNotify(
            myChar,
            session.CharacterId,
            1,
            myPos,
            checked((uint)session.ChannelId),
            session.MapId
        );

        foreach (var other in state.GetAreaPeers(session))
        {
            await other.SendAsync(PacketType.AvatarNotifyData, spawnMeForPeersPacket, ct);
            foreach (var robo in accompanyingRobos)
            {
                var remoteRobo = SharedState.PrepareRemoteRobo(robo, session);
                if (other.VisibleRemoteRoboObjectIds.Add(remoteRobo.Character.SlotId))
                    await other.SendAsync(
                        PacketType.NotifyRoboData,
                        new NotifyRoboData(0, remoteRobo).ToBytes(),
                        ct
                    );
            }

            logger.LogInformation(
                "Sending AvatarNotifyData to {ConnectionId} for othercharacter {CharacterId}",
                other.ConnectionId,
                myChar.Id
            );

            var otherChar = other.Character ?? other.User?.Characters.FirstOrDefault();
            if (otherChar != null)
            {
                var otherPos = new MovementData(
                    other.X,
                    other.Y,
                    other.Z,
                    other.Rotation,
                    MovementType.Stopped
                );
                var spawnOtherForMe = AreasvEnterHandler.CreateNotify(
                    otherChar,
                    other.CharacterId,
                    1,
                    otherPos,
                    checked((uint)other.ChannelId),
                    other.MapId
                );
                await session.SendAsync(PacketType.AvatarNotifyData, spawnOtherForMe, ct);
            }

            if (roboRepository is not null)
            {
                var otherRobos = await roboRepository.GetAllAsync(
                    checked((int)other.CharacterId),
                    ct
                );
                foreach (
                    var robo in otherRobos.Where(x => other.AccompanyingRoboIds.Contains(x.RoboId))
                )
                {
                    var remoteRobo = SharedState.PrepareRemoteRobo(robo, other);
                    if (session.VisibleRemoteRoboObjectIds.Add(remoteRobo.Character.SlotId))
                        await session.SendAsync(
                            PacketType.NotifyRoboData,
                            new NotifyRoboData(0, remoteRobo).ToBytes(),
                            ct
                        );
                }
            }
        }

        await SynchronizeMyRoomOwnerRobosAsync(
            state,
            session,
            roboRepository,
            myRoomRepository,
            ct
        );
    }

    /// <summary>
    /// Visitors never receive the room owner's RoboGetList. Spawn that Charadoll at MapEnter
    /// with the same NotifyRoboData path used for accompanying Robos on every other map.
    /// </summary>
    private static async Task SynchronizeMyRoomOwnerRobosAsync(
        SharedState state,
        IPlayerSession session,
        IRoboRepository? roboRepository,
        IMyRoomRepository? myRoomRepository,
        CancellationToken ct
    )
    {
        if (
            roboRepository is null
            || myRoomRepository is null
            || !MyRoomInfo.IsMyRoomMap(session.MapId)
            || session.MyRoomId == 0
            || session.MyRoomId > int.MaxValue
        )
            return;

        var room = await myRoomRepository.GetRoomAsync(checked((int)session.MyRoomId), ct);
        if (room is null || room.OwnerCharacterId == checked((int)session.CharacterId))
            return;

        var ownerSession = state.GetAreaSessionByCharacterId(
            checked((uint)room.OwnerCharacterId),
            session.MapId,
            session.ChannelId
        );
        if (ownerSession is not null && ownerSession.MyRoomId != session.MyRoomId)
            ownerSession = null;
        var mapSource = ownerSession ?? session;
        var ownerRobos = await roboRepository.GetAllAsync(room.OwnerCharacterId, ct);
        foreach (var robo in ownerRobos)
        {
            var remoteRobo = SharedState.PrepareRemoteRobo(robo, mapSource);
            remoteRobo.OwnerAvatarId = checked((uint)room.OwnerCharacterId);
            if (
                state.TryGetRoboMovement(
                    checked((uint)room.OwnerCharacterId),
                    robo.RoboId,
                    out var lastMovement
                )
            )
            {
                remoteRobo.Character.Map = new CharacterMapData
                {
                    ChannelId = checked((uint)session.ChannelId),
                    MapId = session.MapId,
                    Movement = lastMovement,
                };
            }

            if (!session.VisibleRemoteRoboObjectIds.Add(remoteRobo.Character.SlotId))
                continue;

            await session.SendAsync(
                PacketType.NotifyRoboData,
                new NotifyRoboData(0, remoteRobo).ToBytes(),
                ct
            );
        }
    }
}
