using AISpace.Common.DAL.Entities;
using AISpace.Common.DAL.Repositories;
using AISpace.Common.Game;
using AISpace.Common.Game.ServerScripts;
using AISpace.Network;
using AISpace.Network.Data;
using AISpace.Network.Packets.Area;
using Microsoft.Extensions.Logging;

namespace AISpace.Common.Handlers.Area;

public class AreaMapDataEnterEndHandler(SharedState state, ILogger<AreaMapDataEnterEndHandler> logger, ServerScriptDispatcher? serverScriptDispatcher = null, IRoboRepository? roboRepository = null) : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.MapDataEnterEndRequest;
    public PacketType ResponseType => PacketType.MapDataEnterEndResponse;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, IPlayerSession session, CancellationToken ct = default)
    {
        session.IsMapTransitionPending = false;
        await session.SendAsync(ResponseType, new MapDataEnterEndResponse().ToBytes(), ct);

        var myChar = session.Character ?? session.User!.Characters.FirstOrDefault();
        if (myChar != null)
        {
            var myPos = new MovementData(session.X, session.Y, session.Z, session.Rotation, MovementType.Stopped);
            IReadOnlyList<RoboData> accompanyingRobos = [];
            if (roboRepository is not null)
                accompanyingRobos = (await roboRepository.GetAllAsync(checked((int)session.CharacterId), ct)).Where(x => session.AccompanyingRoboIds.Contains(x.RoboId)).ToList();

            var spawnMeForPeersPacket = AreasvEnterHandler.CreateNotify(myChar, session.CharacterId, 1, myPos);
            if (session.NeedsPostLoadSelfAvatarNotify)
            {
                logger.LogInformation("Sending AvatarNotifyData to {ConnectionId} for character {CharacterId}", session.ConnectionId, myChar.Id);
                var spawnMeForSelfPacket = AreasvEnterHandler.CreateNotify(myChar, session.CharacterId, 0, myPos);
                await session.SendAsync(PacketType.AvatarNotifyData, spawnMeForSelfPacket, ct);
                session.NeedsPostLoadSelfAvatarNotify = false;
            }
            foreach (var other in state.GetAreaPeers(session))
            {
                await other.SendAsync(PacketType.AvatarNotifyData, spawnMeForPeersPacket, ct);
                foreach (var robo in accompanyingRobos)
                {
                    var remoteRobo = SharedState.PrepareRemoteRobo(robo, session);
                    if (other.VisibleRemoteRoboObjectIds.Add(remoteRobo.Character.SlotId))
                        await other.SendAsync(PacketType.NotifyRoboData, new NotifyRoboData(0, remoteRobo).ToBytes(), ct);
                }
                logger.LogInformation("Sending AvatarNotifyData to {ConnectionId} for othercharacter {CharacterId}", other.ConnectionId, myChar.Id);
                var otherChar = other.Character ?? other.User?.Characters.FirstOrDefault();
                if (otherChar != null)
                {
                    var otherPos = new MovementData(other.X, other.Y, other.Z, other.Rotation, MovementType.Stopped);
                    var spawnOtherForMe = AreasvEnterHandler.CreateNotify(otherChar, other.CharacterId, 1, otherPos);
                    await session.SendAsync(PacketType.AvatarNotifyData, spawnOtherForMe, ct);
                }

                if (roboRepository is not null)
                {
                    var otherRobos = await roboRepository.GetAllAsync(checked((int)other.CharacterId), ct);
                    foreach (var robo in otherRobos.Where(x => other.AccompanyingRoboIds.Contains(x.RoboId)))
                    {
                        var remoteRobo = SharedState.PrepareRemoteRobo(robo, other);
                        if (session.VisibleRemoteRoboObjectIds.Add(remoteRobo.Character.SlotId))
                            await session.SendAsync(PacketType.NotifyRoboData, new NotifyRoboData(0, remoteRobo).ToBytes(), ct);
                    }
                }
            }
        }

        // Resume server scripts only after the map load / avatar spawn sequence so client events can start safely.
        if (serverScriptDispatcher is not null)
            await serverScriptDispatcher.TryHandlePacketAsync(RequestType, payload, session, ct);
    }
}
