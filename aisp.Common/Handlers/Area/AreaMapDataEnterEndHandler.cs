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
    ServerScriptDispatcher? serverScriptDispatcher = null,
    SharedState? state = null,
    IRoboRepository? roboRepository = null
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

        // emu2 unlocks the client here before any TPS disappear/reappear work; without it the
        // avatar swap can leave the player event-locked facing the map-change spawn.
        await session.SendAsync(PacketType.EventEndNotify, new EventEndNotify(0).ToBytes(), ct);

        // Prime the client's money manager: balance widgets (the sheet shop, the drama shop) replay its cached
        // values on open, and nothing fills that cache until some window asks for money data.
        if (session.User is not null)
        {
            await session.SendAsync(
                PacketType.MoneyUpdatedAipoint,
                new MoneyUpdatedAipointNotify((ulong)Math.Max(0, session.User.AiPoints)).ToBytes(),
                ct
            );
            await session.SendAsync(
                PacketType.MoneyUpdatedNicopoint,
                new MoneyUpdatedNicopointNotify(
                    (ulong)Math.Max(0, session.User.NicoPoints)
                ).ToBytes(),
                ct
            );
        }

        if (
            session.MapId >= TpsPrototypeConstants.MapIdMinInclusive
            && session.MapId < TpsPrototypeConstants.MapIdMaxExclusive
        )
        {
            await HandleTpsMapEnterAsync(session, ct);
        }
        else
        {
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
        }

        if (state is not null)
        {
            var mapPlacards = state.GetFriendLinkPlacards(
                session.MapId,
                session.ChannelId,
                session.MyRoomId
            );
            await session.SendAsync(
                PacketType.NotifyPlacardInMap,
                new NotifyPlacardInMap(
                    mapPlacards.Select(x => x.ToPacketData()).ToArray()
                ).ToBytes(),
                ct
            );
        }

        // Resume server scripts only after the map load / avatar spawn sequence so client events can start safely.
        if (serverScriptDispatcher is not null)
            await serverScriptDispatcher.TryHandlePacketAsync(RequestType, payload, session, ct);
    }

    private async Task HandleTpsMapEnterAsync(IPlayerSession session, CancellationToken ct)
    {
        // Flag=2 arms the client TPS controller. Swap onto Charadoll immediately so a later
        // AvatarGetDataRequest cannot leave the player stuck as their human avatar.
        session.NeedsPostLoadSelfAvatarNotify = false;
        session.X = TpsPrototypeConstants.PlayerSpawnX;
        session.Y = TpsPrototypeConstants.PlayerSpawnY;
        session.Z = TpsPrototypeConstants.PlayerSpawnZ;
        session.Rotation = 0;

        logger.LogInformation(
            "TPS map {MapId}: entering as Charadoll for character {CharacterId}",
            session.MapId,
            session.CharacterId
        );

        if (roboRepository is not null)
        {
            await TpsCombatEnter.TryEnterAsCharadollAsync(session, roboRepository, logger, ct);
        }
        else
        {
            logger.LogError("TPS map enter skipped: IRoboRepository is not registered");
        }

        // Keep the mode poll for client-side TPS controller handshake (idempotent if already in).
        await session.SendAsync(
            PacketType.EventGetTpsModeNotify,
            new EventGetTpsModeNotify().ToBytes(),
            ct
        );
    }
}
