using aisp.Common.DAL.Entities;
using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Data;
using aisp.Network.Packets.Area;
using aisp.Network.Packets.Common;
using Microsoft.Extensions.Logging;

namespace aisp.Common.Handlers.Area;

public class AreasvEnterHandler(
    IUserSessionRepository _sessionRepo,
    IMapRepository mapRepo,
    IChannelRepository channelRepo,
    ICharacterRepository characterRepo,
    IMyRoomRepository myRoomRepository,
    ICircleRepository circleRepository,
    SharedState state,
    ILogger<AreasvEnterHandler> logger
) : IPacketHandler
{
    private const float SpawnSpread = 50.0f;
    private const int MainChannelNum = 1;

    public PacketType RequestType => PacketType.AreasvEnterRequest;
    public PacketType ResponseType => PacketType.AreasvEnterResponse;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var loginReq = AreasvEnterRequest.FromBytes(payload.Span);
        var userSession = await _sessionRepo.GetValidSessionAsync(loginReq.OTP, ct);

        if (userSession is null || userSession.UserId != loginReq.UserID)
        {
            await session.SendAsync(
                ResponseType,
                new LoginResponse(AuthResponseResult.InvalidCredentials).ToBytes(),
                ct
            );
            return;
        }

        if (userSession.User.IsBanned)
        {
            await session.SendAsync(
                ResponseType,
                new LoginResponse(AuthResponseResult.AccountBanned).ToBytes(),
                ct
            );
            return;
        }

        session.User = userSession.User;
        var chara = await characterRepo.GetByIdAsync(session.User.Characters.First().Id, ct);

        if (chara is null)
        {
            logger.LogWarning(
                "Character not found for UserId={UserId}, sending logout",
                session.User.Id
            );
            await session.SendAsync(PacketType.LogoutNotify, [], ct);
            return;
        }

        uint charId = (uint)chara.Id;

        uint mapId = chara.CurrentMapId;
        var hasPendingTransition = state.TryTakePendingAreaTransition(
            session.User.Id,
            out var pendingTransition
        );
        if (hasPendingTransition)
            mapId = pendingTransition.MapId;

        DAL.Entities.Room? room = null;
        if (MyRoomInfo.IsMyRoomMap(mapId))
        {
            if (hasPendingTransition && pendingTransition.MyRoomId != 0)
                room = await myRoomRepository.GetRoomAsync(
                    checked((int)pendingTransition.MyRoomId),
                    ct
                );
            else if (chara.CurrentRoomId is > 0)
                room = await myRoomRepository.GetRoomAsync(chara.CurrentRoomId.Value, ct);

            if (room is not null)
            {
                var owner =
                    room.OwnerCharacterId == chara.Id
                        ? chara
                        : await characterRepo.GetByIdAsync(room.OwnerCharacterId, ct);
                var sharesCircle =
                    owner is not null
                    && await circleRepository.SharesAnyCircleAsync(chara.Id, owner.Id, ct);
                if (!MyRoomAccess.CanEnter(room, chara.Id, sharesCircle))
                {
                    logger.LogWarning(
                        "AreasvEnter denied My Room {RoomId} for character {CharacterId} (security {Security}); falling back to owner room",
                        room.Id,
                        chara.Id,
                        room.Security
                    );
                    room = null;
                }
            }

            room ??= await myRoomRepository.GetOrCreateDefaultRoomAsync(chara.Id, ct);
            if (room is not null)
                mapId = MyRoomInfo.GetMapId(room.Stage);
        }

        var map = await mapRepo.GetByMapIdAsync(mapId, ct);

        if (!hasPendingTransition && (mapId == 0 || map is null))
        {
            var mainChannel =
                await channelRepo.GetByChannelNumAsync(MainChannelNum, ct)
                ?? (await channelRepo.GetAllAsync(ct)).OrderBy(c => c.ChannelNum).FirstOrDefault();

            if (mainChannel is null)
            {
                logger.LogWarning(
                    "Map not found for MapId={MapId} and no channels are configured; character may spawn at default position.",
                    mapId
                );
            }
            else
            {
                logger.LogInformation(
                    "Falling back to main channel {ChannelId} map {MapId} for user {UserId} (character CurrentMapId was {CurrentMapId})",
                    mainChannel.ChannelNum,
                    mainChannel.MapId,
                    session.User.Id,
                    chara.CurrentMapId
                );
                mapId = mainChannel.MapId;
                map = await mapRepo.GetByMapIdAsync(mapId, ct);
                session.ChannelId = mainChannel.ChannelNum;
                chara = await characterRepo.UpdateCurrentMapAsync(chara.Id, mapId, ct) ?? chara;
            }
        }
        else if (map is null)
        {
            logger.LogWarning(
                "Map not found for MapId={MapId} (character may spawn at default position). Ensure Maps table is seeded on VPS (e.g. volume for main.db or run migration/seed).",
                mapId
            );
        }

        if (hasPendingTransition)
        {
            logger.LogInformation(
                "Applying pending area transition for user {UserId}: map {MapId}, channel {ChannelId}, spawn ({X}, {Y}, {Z}), rotation {Rotation}",
                session.User.Id,
                pendingTransition.MapId,
                pendingTransition.ChannelId,
                pendingTransition.X,
                pendingTransition.Y,
                pendingTransition.Z,
                pendingTransition.Rotation
            );

            session.X = pendingTransition.X;
            session.Y = pendingTransition.Y;
            session.Z = pendingTransition.Z;
            session.Rotation = pendingTransition.Rotation;
            session.ChannelId = pendingTransition.ChannelId;
        }
        else
        {
            if (session.ChannelId == 0)
                session.ChannelId = MainChannelNum;

            float offsetX = (float)(Random.Shared.NextDouble() * 2 * SpawnSpread) - SpawnSpread;
            float offsetZ = (float)(Random.Shared.NextDouble() * 2 * SpawnSpread) - SpawnSpread;

            session.X = (map?.SpawnX ?? 0f) + offsetX;
            session.Y = map?.SpawnY ?? 0.1f;
            session.Z = (map?.SpawnZ ?? 0f) + offsetZ;
            session.Rotation = map?.SpawnRotation ?? 0;
        }

        session.HasMovedSinceMapLoad = false;
        session.IsMapTransitionPending = false;
        session.NeedsPostLoadSelfAvatarNotify = true;
        session.PendingAreaMapSelection = null;
        session.ActiveEventKey = null;
        session.ActiveEventKind = NpcEventKind.None;
        session.ActiveEventCompletionPolicy = EventCompletionPolicy.Once;
        session.ServerScriptState = null;
        session.PendingEventEndAfterFade = false;
        session.AccompanyingRoboIds.Clear();
        session.VisibleRemoteRoboObjectIds.Clear();
        session.Character = chara;
        session.CharacterId = charId;
        session.MapId = mapId;
        session.MyRoomId = room is null ? 0 : checked((uint)room.Id);
        session.PendingMyRoomFurnitureItemId = null;
        session.StorageOpenContext = StorageOpenContext.None;

        state.RegisterClient(ServerType.Area, session);
        await characterRepo.TouchLastLoggedInAsync(chara.Id, ct);
        chara.LastLoggedInAt = DateTime.UtcNow;

        await session.SendAsync(ResponseType, new AreasvEnterResponse(0, charId).ToBytes(), ct);
        // Self avatar: AvatarGetData / MapDataEnterEnd. Peers: MapEnter (post-load).
    }

    public static byte[] CreateNotify(
        DAL.Entities.Character cha,
        uint objId,
        uint res,
        MovementData pos,
        uint channelId = 0,
        uint mapId = 0
    )
    {
        var cd = new CharaData(objId, cha.ModelId, cha.Name)
        {
            Map = new CharacterMapData
            {
                ChannelId = channelId,
                MapId = mapId,
                MapSerialId = mapId,
                RouteState = 0,
                Movement = pos,
            },
        };
        cd.Visual.VisualId = (uint)cha.Id;
        cd.Visual.BloodType = cha.BloodType;
        cd.Visual.Month = (byte)cha.Birthdate.Month;
        cd.Visual.Day = (byte)cha.Birthdate.Day;
        cd.Visual.Gender = (uint)cha.Gender;
        cd.Visual.Face = (byte)cha.FaceType;
        cd.Visual.Hairstyle = cha.Hairstyle;
        cd.AddEquip(
            cha.Equipment.Select(e => new CharacterEquipSlot(e.SlotIndex, (uint)e.ItemId)),
            ItemEntityMapper.ResolveEquipSocket
        );
        return new AvatarNotifyData(res, new AvatarData(objId, cd)).ToBytes();
    }
}
