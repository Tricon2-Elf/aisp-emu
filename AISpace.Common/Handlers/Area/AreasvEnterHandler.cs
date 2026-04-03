using AISpace.Common.DAL.Repositories;
using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Data;
using AISpace.Network.Packets.Area;
using AISpace.Network.Packets.Common;
using Microsoft.Extensions.Logging;

namespace AISpace.Common.Handlers.Area;

public class AreasvEnterHandler(IUserSessionRepository _sessionRepo, IMapRepository mapRepo, ICharacterRepository characterRepo, SharedState state, ILogger<AreasvEnterHandler> logger) : IPacketHandler
{
    private const float SpawnSpread = 50.0f;

    public PacketType RequestType => PacketType.AreasvEnterRequest;
    public PacketType ResponseType => PacketType.AreasvEnterResponse;
    public MessageDomain Domain => MessageDomain.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, IPlayerSession session, CancellationToken ct = default)
    {
        var loginReq = AreasvEnterRequest.FromBytes(payload.Span);
        var userSession = await _sessionRepo.GetValidSessionAsync(loginReq.OTP, ct);

        if (userSession is null || userSession.UserId != loginReq.UserID)
        {
            await session.SendAsync(ResponseType, new LoginResponse(AuthResponseResult.InvalidCredentials).ToBytes(), ct);
            return;
        }

        session.User = userSession.User;
        var chara = await characterRepo.GetByIdAsync(session.User.Characters.First().Id, ct);

        if (chara is null)
        {
            logger.LogWarning("Character not found for UserId={UserId}, sending logout", session.User.Id);
            await session.SendAsync(PacketType.LogoutNotify, [], ct);
            return;
        }

        uint charId = (uint)chara.Id;

        uint mapId = chara.CurrentMapId;
        var map = await mapRepo.GetByMapIdAsync(mapId, ct);

        if (map is null)
        {
            logger.LogWarning("Map not found for MapId={MapId} (character may spawn at default position). Ensure Maps table is seeded on VPS (e.g. volume for main.db or run migration/seed).", mapId);
        }

        float offsetX = (float)(Random.Shared.NextDouble() * 2 * SpawnSpread) - SpawnSpread;
        float offsetZ = (float)(Random.Shared.NextDouble() * 2 * SpawnSpread) - SpawnSpread;

        session.X = (map?.SpawnX ?? 0f) + offsetX;
        session.Y = map?.SpawnY ?? 0.1f;
        session.Z = (map?.SpawnZ ?? 0f) + offsetZ;
        session.Rotation = (sbyte)(map?.SpawnRotation ?? 0);
        session.Character = chara;
        session.CharacterId = charId;
        session.MapId = mapId;

        state.RegisterClient("Area", session);

        await session.SendAsync(ResponseType, new AreasvEnterResponse(0, charId).ToBytes(), ct);

        _ = Task.Run(
            async () =>
            {
                await Task.Delay(1000, ct);

                var cha = session.Character ?? session.User!.Characters.First();
                var myPos = new MovementData(session.X, session.Y, session.Z, session.Rotation, MovementType.Stopped);

                await session.SendAsync(PacketType.AvatarNotifyData, CreateNotify(cha, charId, 0, myPos), ct);

                foreach (var other in state.GetAreaPeers(session))
                {
                    await other.SendAsync(PacketType.AvatarNotifyData, CreateNotify(cha, charId, 1, myPos), ct);

                    var oCha = other.Character ?? other.User?.Characters.FirstOrDefault();
                    if (oCha != null)
                    {
                        var oPos = new MovementData(other.X, other.Y, other.Z, other.Rotation, MovementType.Stopped);
                        await session.SendAsync(PacketType.AvatarNotifyData, CreateNotify(oCha, other.CharacterId, 1, oPos), ct);
                    }
                }
            },
            ct
        );
    }

    public static byte[] CreateNotify(DAL.Entities.Character cha, uint objId, uint res, MovementData pos)
    {
        var cd = new CharaData(objId, cha.ModelId, cha.Name) { moveData = pos };
        cd.Visual.VisualId = (uint)cha.Id;
        cd.Visual.BloodType = cha.BloodType;
        cd.Visual.Month = (byte)cha.Birthdate.Month;
        cd.Visual.Day = (byte)cha.Birthdate.Day;
        cd.Visual.Gender = (uint)cha.Gender;
        cd.Visual.Face = (byte)cha.FaceType;
        cd.Visual.Hairstyle = cha.Hairstyle;
        foreach (var eq in cha.Equipment)
            cd.AddEquip((uint)eq.ItemId, eq.SlotIndex);
        return new AvatarNotifyData(res, new AvatarData(objId, cd)).ToBytes();
    }
}
