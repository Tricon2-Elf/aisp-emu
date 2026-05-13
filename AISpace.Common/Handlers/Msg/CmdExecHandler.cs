using AISpace.Common.DAL.Repositories;
using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Data;
using AISpace.Network.Packets.Area;
using AISpace.Network.Packets.Msg;
using Microsoft.Extensions.Logging;
using Character = AISpace.Common.DAL.Entities.Character;

namespace AISpace.Common.Handlers.Msg;

public class CmdExecHandler(ISessionPresenceRepository presenceRepo, SharedState state, IMapRepository mapRepo, ILogger<CmdExecHandler> logger) : IPacketHandler, IRequiresAuthenticatedSession
{
    private const float SpawnSpread = 50.0f;

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
            var presence = presenceRepo.GetAreaSessionByCharacterId(session.CharacterId);
            if (presence != null)
            {
                logger.LogCritical("\n" + "==========================================\n" + $"  LOCATION DATA for Char: {presence.CharacterId}\n" + $"  X: {presence.X}f\n" + $"  Y: {presence.Y}f\n" + $"  Z: {presence.Z}f\n" + $"  Rotation: {presence.Rotation}\n" + "==========================================");
            }
            else
            {
                logger.LogWarning("CmdExecHandler: No area session found for character {CharacterId} (server may be in separate process)", session.CharacterId);
            }
            return;
        }

        if (cmd == "escape" || cmd == "reset")
        {
            var areaClient = state.GetAreaSessionByCharacterId(session.CharacterId);

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
                logger.LogWarning("CmdExecHandler: escape command requires Area server in the same process (character {CharacterId})", session.CharacterId);
            }
        }
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
