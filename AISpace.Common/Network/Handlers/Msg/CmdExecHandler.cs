using AISpace.Common.DAL.Entities;
using AISpace.Common.DAL.Repositories;
using AISpace.Common.Game;
using AISpace.Common.Network.Packets.Area;
using AISpace.Common.Network.Packets.Msg;
using Microsoft.Extensions.Logging;

namespace AISpace.Common.Network.Handlers.Msg;

public class CmdExecHandler(SharedState state, IMapRepository mapRepo, ILogger<CmdExecHandler> logger) : IPacketHandler
{
    private const float SpawnSpread = 50.0f;

    public PacketType RequestType => PacketType.CmdExecRequest;
    public PacketType ResponseType => PacketType.CmdExecResponse;
    public MessageDomain Domain => MessageDomain.Msg;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, ClientConnection connection, CancellationToken ct = default)
    {
        var request = CmdExecRequest.FromBytes(payload.Span);

        var response = new CmdExecResponse(request.MessageId, 0);
        await connection.SendAsync(ResponseType, response.ToBytes(), ct);

        string cmd = request.Command.ToLower();

        if (cmd == "pos" || cmd == "coords")
        {
            var areaClient = state.AreaClients.Values.FirstOrDefault(c => c.CharacterId == connection.CharacterId);
            if (areaClient != null)
            {
                logger.LogCritical("\n" + "==========================================\n" + $"  LOCATION DATA for Char: {areaClient.CharacterId}\n" + $"  X: {areaClient.X}f\n" + $"  Y: {areaClient.Y}f\n" + $"  Z: {areaClient.Z}f\n" + $"  Rotation: {areaClient.Rotation}\n" + "==========================================");
            }
            return;
        }

        if (cmd == "escape" || cmd == "reset")
        {
            var areaClient = state.AreaClients.Values.FirstOrDefault(c => c.CharacterId == connection.CharacterId);

            if (areaClient != null && areaClient.User != null && areaClient.User.Characters.Count > 0)
            {
                var chara = areaClient.User.Characters.First();
                uint mapId = chara.CurrentMapId;

                var map = await mapRepo.GetByMapIdAsync(mapId, ct);

                float offsetX = (float)(Random.Shared.NextDouble() * 2 * SpawnSpread) - SpawnSpread;
                float offsetZ = (float)(Random.Shared.NextDouble() * 2 * SpawnSpread) - SpawnSpread;

                areaClient.X = (map?.SpawnX ?? 0f) + offsetX;
                areaClient.Y = map?.SpawnY ?? 0.1f;
                areaClient.Z = (map?.SpawnZ ?? 0f) + offsetZ;
                areaClient.Rotation = (sbyte)(map?.SpawnRotation ?? 0);
                areaClient.CurrentAnimation = MovementType.Stopped;

                var newPos = new MovementData(areaClient.X, areaClient.Y, areaClient.Z, areaClient.Rotation, MovementType.Stopped);

                var notifyMove = new AvatarNotifyMove(1, areaClient.CharacterId, newPos).ToBytes();
                await areaClient.SendAsync(PacketType.AvatarNotifyMove, notifyMove, ct);

                var disappearPacket = new NotifyDisappearChara(areaClient.CharacterId).ToBytes();
                var appearPacket = CreateTeleportNotify(chara, areaClient.CharacterId, newPos);

                foreach (var other in state.AreaClients.Values)
                {
                    if (other.Id == areaClient.Id)
                        continue;

                    await other.SendAsync(PacketType.NotifyDisappearChara, disappearPacket, ct);
                    await other.SendAsync(PacketType.AvatarNotifyData, appearPacket, ct);
                }
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
