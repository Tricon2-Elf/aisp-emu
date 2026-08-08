using AISpace.Common.DAL.Repositories;
using AISpace.Common.Game;
using AISpace.Common.Handlers.Area;
using AISpace.Common.Services;
using AISpace.Network;
using AISpace.Network.Data;
using AISpace.Network.Packets.Area;
using AISpace.Network.Packets.Msg;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Character = AISpace.Common.DAL.Entities.Character;

namespace AISpace.Common.Handlers.Msg;

public class CmdExecHandler(
    SharedState state,
    IMapRepository mapRepo,
    IUserRepository userRepo,
    ICharacterRepository characterRepo,
    IMyRoomRepository myRoomRepository,
    ICircleRepository circleRepository,
    IItemBaseListCache itemBaseListCache,
    DirectMapLinkTransitionService directMapLinkTransitionService,
    ILogger<CmdExecHandler> logger
) : IPacketHandler, IRequiresAuthenticatedSession
{
    private const float SpawnSpread = 50.0f;
    private const float JumpDistance = 100f;

    public PacketType RequestType => PacketType.CmdExecRequest;
    public PacketType ResponseType => PacketType.CmdExecResponse;
    public ServerType ServerType => ServerType.Msg;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var request = CmdExecRequest.FromBytes(payload.Span);

        var response = new CmdExecResponse(request.MessageId, 0);
        await session.SendAsync(ResponseType, response.ToBytes(), ct);
        string cmd = request.Command.Trim().TrimStart('/').ToLowerInvariant();
        logger.LogInformation(
            "CmdExecHandler: '{cmd}' with args: '{args}'",
            cmd,
            string.Join(", ", request.Arguments)
        );

        if (cmd is "pos" or "coords")
        {
            var areaClient = ResolveAreaClient(session);
            if (areaClient != null)
            {
                // DistID -5 is the client "System" / Notice chat filter (see sub_428B10 / sub_428BB0).
                var text =
                    $"Char: {areaClient.CharacterId} | Map: {areaClient.MapId} | Ch: {areaClient.ChannelId} | X: {areaClient.X}f | Y: {areaClient.Y}f | Z: {areaClient.Z}f | Rot: {areaClient.Rotation}";
                await SendSystemNoticeAsync(session, text, ct);
            }
            else
            {
                logger.LogWarning(
                    "CmdExecHandler: No area session found for user {UserId} (server may be in separate process or not in area)",
                    session.User?.Id ?? session.UserId
                );
            }
            return;
        }

        if (cmd is "tele" or "tp" or "teleport")
        {
            var destinationMapId = 10990100u;
            if (
                request.Arguments.Count == 0
                || !uint.TryParse(request.Arguments[0], out destinationMapId)
            )
            {
                destinationMapId = 10990100u;
            }

            var areaClient = ResolveAreaClient(session);
            if (areaClient == null)
            {
                logger.LogWarning(
                    "CmdExecHandler: tele requires an active area session for user {UserId}",
                    session.User?.Id ?? session.UserId
                );
                return;
            }

            if (
                !await directMapLinkTransitionService.TryTeleportToMapAsync(
                    areaClient,
                    destinationMapId,
                    ct
                )
            )
            {
                logger.LogWarning(
                    "CmdExecHandler: tele to map {MapId} failed for user {UserId} (character {CharacterId})",
                    destinationMapId,
                    session.User?.Id ?? session.UserId,
                    areaClient.CharacterId
                );
            }
            else
            {
                logger.LogInformation(
                    "CmdExecHandler: teleported user {UserId} (character {CharacterId}) to map {MapId}",
                    session.User?.Id ?? session.UserId,
                    areaClient.CharacterId,
                    destinationMapId
                );
            }

            return;
        }

        if (cmd is "myroom" or "room")
        {
            var areaClient = ResolveAreaClient(session);
            if (areaClient == null || areaClient.CharacterId == 0)
            {
                logger.LogWarning(
                    "CmdExecHandler: myroom requires an active area session for user {UserId}",
                    session.User?.Id ?? session.UserId
                );
                return;
            }

            var character = await characterRepo.GetByIdAsync(
                checked((int)areaClient.CharacterId),
                ct
            );
            if (character is null || character.HomeIslandId == 0)
            {
                logger.LogWarning(
                    "CmdExecHandler: myroom requires character {CharacterId} to have a home island",
                    areaClient.CharacterId
                );
                return;
            }

            areaClient.Character = character;

            DAL.Entities.Room? room;
            if (
                cmd == "room"
                && request.Arguments.Count > 0
                && string.Equals(request.Arguments[0], "create", StringComparison.OrdinalIgnoreCase)
            )
            {
                if (
                    !TryParseRoomStage(
                        request.Arguments.Count > 1 ? request.Arguments[1] : null,
                        out var stage
                    )
                )
                {
                    logger.LogWarning(
                        "CmdExecHandler: room create requires a tatami size of 6, 8, 10, or 12 for character {CharacterId}",
                        areaClient.CharacterId
                    );
                    return;
                }

                var roomName =
                    request.Arguments.Count > 2
                        ? string.Join(' ', request.Arguments.Skip(2))
                        : "My Room";
                if (roomName.Length > 45)
                {
                    logger.LogWarning(
                        "CmdExecHandler: room create name is longer than 45 characters for character {CharacterId}",
                        areaClient.CharacterId
                    );
                    return;
                }

                room = await myRoomRepository.CreateRoomAsync(character.Id, stage, roomName, ct);
                if (room is null)
                {
                    logger.LogWarning(
                        "CmdExecHandler: failed to create room for character {CharacterId}",
                        areaClient.CharacterId
                    );
                    return;
                }
            }
            else if (cmd == "room" && request.Arguments.Count > 0)
            {
                if (
                    !long.TryParse(request.Arguments[0], out var parsedRoomId)
                    || parsedRoomId <= 0
                    || parsedRoomId > int.MaxValue
                )
                {
                    await SendSystemNoticeAsync(
                        session,
                        $"Invalid room ID. Use a number from 1 to {int.MaxValue}.",
                        ct
                    );
                    logger.LogWarning(
                        "CmdExecHandler: room requires a positive room ID for character {CharacterId} (got '{Argument}')",
                        areaClient.CharacterId,
                        request.Arguments[0]
                    );
                    return;
                }

                var roomId = checked((int)parsedRoomId);
                room = await myRoomRepository.GetRoomAsync(roomId, ct);
                if (room is null)
                {
                    await SendSystemNoticeAsync(session, $"Room {roomId} does not exist.", ct);
                    logger.LogWarning(
                        "CmdExecHandler: room {RoomId} does not exist for character {CharacterId}",
                        roomId,
                        areaClient.CharacterId
                    );
                    return;
                }

                if (room.OwnerCharacterId != character.Id)
                {
                    var owner = await characterRepo.GetByIdAsync(room.OwnerCharacterId, ct);
                    var sharesCircle =
                        owner is not null
                        && await circleRepository.SharesAnyCircleAsync(character.Id, owner.Id, ct);
                    if (!MyRoomAccess.CanEnter(room, character.Id, sharesCircle))
                    {
                        var message =
                            room.Security == MyRoomSecurity.Private
                                ? "You can't join that room because it is Private."
                                : "You can't join that room.";
                        await SendSystemNoticeAsync(session, message, ct);
                        logger.LogWarning(
                            "CmdExecHandler: denied room {RoomId} for character {CharacterId} (security {Security})",
                            room.Id,
                            character.Id,
                            room.Security
                        );
                        return;
                    }
                }
            }
            else
            {
                room = await myRoomRepository.GetOrCreateDefaultRoomAsync(character.Id, ct);
                if (room is null)
                {
                    logger.LogWarning(
                        "CmdExecHandler: could not resolve the default room for character {CharacterId}",
                        areaClient.CharacterId
                    );
                    return;
                }
            }

            if (!await directMapLinkTransitionService.TryTeleportToRoomAsync(areaClient, room, ct))
                logger.LogWarning(
                    "CmdExecHandler: room teleport failed for user {UserId} (character {CharacterId}, room {RoomId})",
                    session.User?.Id ?? session.UserId,
                    areaClient.CharacterId,
                    room.Id
                );
            else
                logger.LogInformation(
                    "CmdExecHandler: teleported user {UserId} (character {CharacterId}) to room {RoomId} owned by character {OwnerCharacterId} on stage {Stage}",
                    session.User?.Id ?? session.UserId,
                    areaClient.CharacterId,
                    room.Id,
                    room.OwnerCharacterId,
                    room.Stage
                );

            return;
        }

        if (cmd is "jump")
        {
            var areaClient = ResolveAreaClient(session);
            if (
                areaClient == null
                || areaClient.User == null
                || areaClient.User.Characters.Count == 0
            )
            {
                logger.LogWarning(
                    "CmdExecHandler: jump requires an active area session for user {UserId}",
                    session.User?.Id ?? session.UserId
                );
                return;
            }

            var jumpDistance = JumpDistance;
            if (
                request.Arguments.Count > 0
                && float.TryParse(request.Arguments[0], out var parsedDistance)
            )
                jumpDistance = parsedDistance;

            var angle = areaClient.Rotation * (MathF.PI / 180f);
            // Character forward matches maplink normal: (Sin, Cos). (Cos, -Sin) is strafe/right.
            areaClient.X += MathF.Sin(angle) * jumpDistance;
            areaClient.Z += MathF.Cos(angle) * jumpDistance;
            areaClient.MovementTypeId = (int)MovementType.Stopped;

            var chara = areaClient.Character ?? areaClient.User.Characters.First();
            var newPos = new MovementData(
                areaClient.X,
                areaClient.Y,
                areaClient.Z,
                areaClient.Rotation,
                MovementType.Stopped
            );

            var notifyMove = new AvatarNotifyMove(areaClient.CharacterId, [newPos]).ToBytes();
            await areaClient.SendAsync(PacketType.AvatarNotifyMove, notifyMove, ct);

            var disappearPacket = new NotifyDisappearChara(areaClient.CharacterId).ToBytes();
            var appearPacket = CreateTeleportNotify(chara, areaClient.CharacterId, newPos);

            foreach (var other in state.GetAreaPeers(areaClient))
            {
                await other.SendAsync(PacketType.NotifyDisappearChara, disappearPacket, ct);
                await other.SendAsync(PacketType.AvatarNotifyData, appearPacket, ct);
            }

            return;
        }

        if (cmd is "outfit" or "starter" or "starteroutfit")
        {
            var areaClient = ResolveAreaClient(session);
            if (areaClient == null || areaClient.CharacterId == 0)
            {
                logger.LogWarning(
                    "CmdExecHandler: outfit requires an active area session for user {UserId}",
                    session.User?.Id ?? session.UserId
                );
                return;
            }

            var characterId = (int)areaClient.CharacterId;
            var character = await characterRepo.GetByIdAsync(characterId, ct);
            if (character is null)
            {
                logger.LogWarning(
                    "CmdExecHandler: outfit could not resolve character {CharacterId}",
                    characterId
                );
                return;
            }

            var itemIds = DefaultClothingItems
                .WardrobeInventoryForGender(character.Gender)
                .ToList();

            foreach (var itemId in itemIds)
                await characterRepo.AddInventoryAsync(characterId, itemId, 1, ct);

            var refreshed = await characterRepo.GetByIdAsync(characterId, ct);
            if (refreshed is null)
                return;

            areaClient.Character = refreshed;
            await CharacterItemSync.SendInventoryBootstrapAsync(areaClient, refreshed, ct);

            logger.LogInformation(
                "CmdExecHandler: added default outfit ({Count} wardrobe items) to inventory for character {CharacterId} and synced to area client",
                itemIds.Count,
                characterId
            );
            return;
        }

        if (cmd is "give")
        {
            var areaClient = ResolveAreaClient(session);
            if (areaClient == null || areaClient.CharacterId == 0)
            {
                logger.LogWarning(
                    "CmdExecHandler: give requires an active area session for user {UserId}",
                    session.User?.Id ?? session.UserId
                );
                return;
            }

            if (
                request.Arguments.Count == 0
                || !int.TryParse(request.Arguments[0], out var itemId)
                || itemId <= 0
            )
            {
                logger.LogWarning(
                    "CmdExecHandler: give requires a positive item id argument (user {UserId})",
                    session.User?.Id ?? session.UserId
                );
                return;
            }

            if (!await itemBaseListCache.ContainsItemAsync(itemId, ct))
            {
                logger.LogWarning(
                    "CmdExecHandler: give rejected unknown item {ItemId} for character {CharacterId}",
                    itemId,
                    areaClient.CharacterId
                );
                return;
            }

            var quantity = 1;
            if (
                request.Arguments.Count > 1
                && int.TryParse(request.Arguments[1], out var parsedQuantity)
                && parsedQuantity > 0
            )
                quantity = parsedQuantity;

            var characterId = (int)areaClient.CharacterId;
            var character = await characterRepo.GetByIdAsync(characterId, ct);
            if (character is null)
            {
                logger.LogWarning(
                    "CmdExecHandler: give could not resolve character {CharacterId}",
                    characterId
                );
                return;
            }

            var previousQuantity =
                character.Inventory.FirstOrDefault(i => i.ItemId == itemId)?.Quantity ?? 0;

            try
            {
                await characterRepo.AddInventoryAsync(characterId, itemId, quantity, ct);
            }
            catch (DbUpdateException ex)
            {
                logger.LogWarning(
                    ex,
                    "CmdExecHandler: give failed to add item {ItemId} (qty {Quantity}) to character {CharacterId}",
                    itemId,
                    quantity,
                    characterId
                );
                return;
            }

            var totalQuantity = (ushort)Math.Clamp(previousQuantity + quantity, 0, ushort.MaxValue);
            await CharacterItemSync.SendInventoryItemAsync(areaClient, itemId, totalQuantity, ct);

            var refreshed = await characterRepo.GetByIdAsync(characterId, ct);
            if (refreshed is not null)
                areaClient.Character = refreshed;

            logger.LogInformation(
                "CmdExecHandler: gave item {ItemId} x{Quantity} to character {CharacterId} and sent inventory notify",
                itemId,
                quantity,
                characterId
            );
            return;
        }

        if (cmd is "money")
        {
            var userId = session.User?.Id ?? session.UserId;
            if (userId <= 0)
            {
                logger.LogWarning("CmdExecHandler: money requires an authenticated user");
                return;
            }

            if (
                request.Arguments.Count == 0
                || !long.TryParse(request.Arguments[0], out var amount)
                || amount <= 0
            )
            {
                logger.LogWarning(
                    "CmdExecHandler: money requires a positive amount argument (user {UserId})",
                    userId
                );
                return;
            }

            var target = "both";
            if (request.Arguments.Count > 1)
                target = request.Arguments[1].Trim().ToLowerInvariant();

            var addAiPoints = target is "both" or "all" or "ai" or "aipoints";
            var addNicoPoints = target is "both" or "all" or "nico" or "nicopoints";
            if (!addAiPoints && !addNicoPoints)
            {
                logger.LogWarning(
                    "CmdExecHandler: money unsupported target '{Target}' for user {UserId} (expected ai|nico|both)",
                    target,
                    userId
                );
                return;
            }

            var aiDelta = addAiPoints ? amount : 0;
            var nicoDelta = addNicoPoints ? amount : 0;
            var user = await userRepo.AddMoneyAsync(userId, aiDelta, nicoDelta, ct);
            if (user is null)
            {
                logger.LogWarning("CmdExecHandler: money could not resolve user {UserId}", userId);
                return;
            }

            session.User = user;
            var areaClient = ResolveAreaClient(session);
            if (areaClient?.User != null)
            {
                areaClient.User.AiPoints = user.AiPoints;
                areaClient.User.NicoPoints = user.NicoPoints;
            }

            var notifySession = areaClient ?? session;
            await notifySession.SendAsync(
                PacketType.MoneyUpdatedAipoint,
                new MoneyUpdatedAipointNotify((ulong)Math.Max(0, user.AiPoints)).ToBytes(),
                ct
            );
            await notifySession.SendAsync(
                PacketType.MoneyUpdatedNicopoint,
                new MoneyUpdatedNicopointNotify((ulong)Math.Max(0, user.NicoPoints)).ToBytes(),
                ct
            );

            logger.LogInformation(
                "CmdExecHandler: added {Amount} points ({Target}) for user {UserId} => ai={AiPoints}, nico={NicoPoints}",
                amount,
                target,
                userId,
                user.AiPoints,
                user.NicoPoints
            );
            return;
        }

        if (cmd is "escape" or "reset")
        {
            var areaClient = ResolveAreaClient(session);

            if (
                areaClient != null
                && areaClient.User != null
                && areaClient.User.Characters.Count > 0
            )
            {
                var chara = areaClient.Character ?? areaClient.User.Characters.First();
                uint mapId = chara.CurrentMapId;

                var map = await mapRepo.GetByMapIdAsync(mapId, ct);

                float offsetX = (float)(Random.Shared.NextDouble() * 2 * SpawnSpread) - SpawnSpread;
                float offsetZ = (float)(Random.Shared.NextDouble() * 2 * SpawnSpread) - SpawnSpread;

                areaClient.X = (map?.SpawnX ?? 0f) + offsetX;
                areaClient.Y = map?.SpawnY ?? 0.1f;
                areaClient.Z = (map?.SpawnZ ?? 0f) + offsetZ;
                areaClient.Rotation = map?.SpawnRotation ?? 0;
                areaClient.MovementTypeId = (int)MovementType.Stopped;

                var newPos = new MovementData(
                    areaClient.X,
                    areaClient.Y,
                    areaClient.Z,
                    areaClient.Rotation,
                    MovementType.Stopped
                );

                var notifyMove = new AvatarNotifyMove(areaClient.CharacterId, [newPos]).ToBytes();
                await areaClient.SendAsync(PacketType.AvatarNotifyMove, notifyMove, ct);

                var disappearPacket = new NotifyDisappearChara(areaClient.CharacterId).ToBytes();
                var appearPacket = CreateTeleportNotify(chara, areaClient.CharacterId, newPos);

                foreach (var other in state.GetAreaPeers(areaClient))
                {
                    await other.SendAsync(PacketType.NotifyDisappearChara, disappearPacket, ct);
                    await other.SendAsync(PacketType.AvatarNotifyData, appearPacket, ct);
                }
            }
            else
            {
                logger.LogWarning(
                    "CmdExecHandler: escape requires an active area session for user {UserId}",
                    session.User?.Id ?? session.UserId
                );
            }
        }
    }

    private static bool TryParseRoomStage(string? value, out MyRoomStage stage)
    {
        stage = value switch
        {
            "6" => MyRoomStage.SixTatami,
            "8" => MyRoomStage.EightTatami,
            "10" => MyRoomStage.TenTatami,
            "12" => MyRoomStage.TwelveTatami,
            _ => (MyRoomStage)byte.MaxValue,
        };
        return Enum.IsDefined(stage);
    }

    private IPlayerSession? ResolveAreaClient(IPlayerSession msgSession)
    {
        var userId = msgSession.User?.Id ?? msgSession.UserId;
        if (userId != 0)
        {
            var byUser = state.GetAreaSessionByUserId(userId);
            if (byUser != null)
                return byUser;
        }

        if (msgSession.CharacterId != 0)
            return state.GetAreaSessionByCharacterId(msgSession.CharacterId);

        return null;
    }

    private static Task SendSystemNoticeAsync(
        IPlayerSession session,
        string text,
        CancellationToken ct
    )
    {
        // DistID -5 is the client "System" / Notice chat filter (see sub_428B10 / sub_428BB0).
        const uint systemDistId = unchecked((uint)-5);
        return session.SendAsync(
            PacketType.TalkForwardNotify,
            new TalkForwardNotify(0, systemDistId, text, 0).ToBytes(),
            ct
        );
    }

    private static byte[] CreateTeleportNotify(Character cha, uint objId, MovementData pos)
    {
        var cd = new CharaData(objId, cha.ModelId, cha.Name) { Movement = pos };
        cd.Visual.VisualId = objId;
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
        return new AvatarNotifyData(1, new AvatarData(objId, cd)).ToBytes();
    }
}
