using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Common.Game.ServerScripts;
using aisp.Network;
using aisp.Network.Data;
using aisp.Network.Packets.Area;
using Microsoft.Extensions.Logging;

namespace aisp.Common.Handlers.Area;

public class AreaMapDataEnterEndHandler(
    ILogger<AreaMapDataEnterEndHandler> logger,
    ServerScriptDispatcher? serverScriptDispatcher = null
) : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.MapDataEnterEndRequest;
    public PacketType ResponseType => PacketType.MapDataEnterEndResponse;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        session.IsMapTransitionPending = false;
        await session.SendAsync(ResponseType, new MapDataEnterEndResponse().ToBytes(), ct);

        // Self avatar only here. Peer/robo presence waits for MapEnter so the client
        // finishes map load before remote avatars arrive (room-functions 2-player crash).
        var myChar = session.Character ?? session.User!.Characters.FirstOrDefault();
        if (myChar != null && session.NeedsPostLoadSelfAvatarNotify)
        {
            logger.LogInformation(
                "Sending AvatarNotifyData to {ConnectionId} for character {CharacterId}",
                session.ConnectionId,
                myChar.Id
            );
            var myPos = new MovementData(
                session.X,
                session.Y,
                session.Z,
                session.Rotation,
                MovementType.Stopped
            );
            var spawnMeForSelfPacket = AreasvEnterHandler.CreateNotify(
                myChar,
                session.CharacterId,
                0,
                myPos,
                checked((uint)session.ChannelId),
                session.MapId
            );
            await session.SendAsync(PacketType.AvatarNotifyData, spawnMeForSelfPacket, ct);
            session.NeedsPostLoadSelfAvatarNotify = false;
        }

        // Resume server scripts only after the map load / avatar spawn sequence so client events can start safely.
        if (serverScriptDispatcher is not null)
            await serverScriptDispatcher.TryHandlePacketAsync(RequestType, payload, session, ct);
    }
}
