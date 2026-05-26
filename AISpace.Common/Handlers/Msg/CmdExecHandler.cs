using AISpace.Common.DAL.Repositories;
using AISpace.Common.Game;
using AISpace.Common.Handlers.Area;
using AISpace.Network;
using AISpace.Network.Data;
using AISpace.Network.Packets.Area;
using AISpace.Network.Packets.Msg;
using Microsoft.Extensions.Logging;
using Character = AISpace.Common.DAL.Entities.Character;

namespace AISpace.Common.Handlers.Msg;

public class CmdExecHandler(SharedState state, IMapRepository mapRepo, DirectMapLinkTransitionService directMapLinkTransitionService, ILogger<CmdExecHandler> logger) : IPacketHandler, IRequiresAuthenticatedSession
{
    private const float SpawnSpread = 50.0f;
    private const float JumpDistance = 100f;

    public PacketType RequestType => PacketType.CmdExecRequest;
    public PacketType ResponseType => PacketType.CmdExecResponse;
    public ServerType ServerType => ServerType.Msg;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, IPlayerSession session, CancellationToken ct = default)
    {
        var request = CmdExecRequest.FromBytes(payload.Span);

        var response = new CmdExecResponse(request.MessageId, 0);
        await session.SendAsync(ResponseType, response.ToBytes(), ct);
        string cmd = request.Command.Trim().ToLower();
        logger.LogInformation("CmdExecHandler: '{cmd}' with args: '{args}'", cmd, string.Join(", ", request.Arguments));

        if (cmd == "pos" || cmd == "coords")
        {
            var areaClient = ResolveAreaClient(session);
            if (areaClient != null)
            {
                logger.LogCritical("\n" + "==========================================\n" + $"  LOCATION DATA for Char: {areaClient.CharacterId}\n" + $"  Map: {areaClient.MapId}\n" + $"  Channel: {areaClient.ChannelId}\n" + $"  X: {areaClient.X}f\n" + $"  Y: {areaClient.Y}f\n" + $"  Z: {areaClient.Z}f\n" + $"  Rotation: {areaClient.Rotation}\n" + "==========================================");
            }
            else
            {
                logger.LogWarning("CmdExecHandler: No area session found for user {UserId} (server may be in separate process or not in area)", session.User?.Id ?? session.UserId);
            }
            return;
        }

        if (cmd == "tele" || cmd == "tp" || cmd == "teleport")
        {
            var destinationMapId = 10990100u;
            if (request.Arguments.Count == 0 || !uint.TryParse(request.Arguments[0], out destinationMapId))
            {
                destinationMapId = 10990100u;
            }

            var areaClient = ResolveAreaClient(session);
            if (areaClient == null)
            {
                logger.LogWarning("CmdExecHandler: tele requires an active area session for user {UserId}", session.User?.Id ?? session.UserId);
                return;
            }

            if (!await directMapLinkTransitionService.TryTeleportToMapAsync(areaClient, destinationMapId, ct))
            {
                logger.LogWarning("CmdExecHandler: tele to map {MapId} failed for user {UserId} (character {CharacterId})", destinationMapId, session.User?.Id ?? session.UserId, areaClient.CharacterId);
            }
            else
            {
                logger.LogInformation("CmdExecHandler: teleported user {UserId} (character {CharacterId}) to map {MapId}", session.User?.Id ?? session.UserId, areaClient.CharacterId, destinationMapId);
            }

            return;
        }

        if (cmd == "jump")
        {
            var areaClient = ResolveAreaClient(session);
            if (areaClient == null || areaClient.User == null || areaClient.User.Characters.Count == 0)
            {
                logger.LogWarning("CmdExecHandler: jump requires an active area session for user {UserId}", session.User?.Id ?? session.UserId);
                return;
            }

            var jumpDistance = JumpDistance;
            if (request.Arguments.Count > 0 && float.TryParse(request.Arguments[0], out var parsedDistance))
                jumpDistance = parsedDistance;

            var angle = areaClient.Rotation * (MathF.PI / 180f);
            areaClient.X += MathF.Cos(angle) * jumpDistance;
            areaClient.Z += -MathF.Sin(angle) * jumpDistance;
            areaClient.MovementTypeId = (int)MovementType.Stopped;

            var chara = areaClient.Character ?? areaClient.User.Characters.First();
            var newPos = new MovementData(areaClient.X, areaClient.Y, areaClient.Z, areaClient.Rotation, MovementType.Stopped);

            var notifyMove = new AvatarNotifyMove(1, areaClient.CharacterId, newPos).ToBytes();
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

        if (cmd == "escape" || cmd == "reset")
        {
            var areaClient = ResolveAreaClient(session);

            if (areaClient != null && areaClient.User != null && areaClient.User.Characters.Count > 0)
            {
                var chara = areaClient.Character ?? areaClient.User.Characters.First();
                uint mapId = chara.CurrentMapId;

                var map = await mapRepo.GetByMapIdAsync(mapId, ct);

                float offsetX = (float)(Random.Shared.NextDouble() * 2 * SpawnSpread) - SpawnSpread;
                float offsetZ = (float)(Random.Shared.NextDouble() * 2 * SpawnSpread) - SpawnSpread;

                areaClient.X = (map?.SpawnX ?? 0f) + offsetX;
                areaClient.Y = map?.SpawnY ?? 0.1f;
                areaClient.Z = (map?.SpawnZ ?? 0f) + offsetZ;
                areaClient.Rotation = (sbyte)(map?.SpawnRotation ?? 0);
                areaClient.MovementTypeId = (int)MovementType.Stopped;

                var newPos = new MovementData(areaClient.X, areaClient.Y, areaClient.Z, areaClient.Rotation, MovementType.Stopped);

                var notifyMove = new AvatarNotifyMove(1, areaClient.CharacterId, newPos).ToBytes();
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
                logger.LogWarning("CmdExecHandler: escape requires an active area session for user {UserId}", session.User?.Id ?? session.UserId);
            }
        }
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

    private static byte[] CreateTeleportNotify(Character cha, uint objId, MovementData pos)
    {
        var cd = new CharaData(objId, cha.ModelId, cha.Name) { moveData = pos };
        cd.Visual.VisualId = objId;
        cd.Visual.BloodType = cha.BloodType;
        cd.Visual.Month = (byte)cha.Birthdate.Month;
        cd.Visual.Day = (byte)cha.Birthdate.Day;
        cd.Visual.Gender = (uint)cha.Gender;
        cd.Visual.Face = (byte)cha.FaceType;
        cd.Visual.Hairstyle = cha.Hairstyle;
        foreach (var eq in cha.Equipment)
            cd.AddEquip((uint)eq.ItemId, eq.SlotIndex);
        return new AvatarNotifyData(1, new AvatarData(objId, cd)).ToBytes();
    }
}
