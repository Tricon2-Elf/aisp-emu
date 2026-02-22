using AISpace.Common.DAL.Repositories;
using AISpace.Common.Game;
using AISpace.Common.Network;
using AISpace.Common.Network.Packets.Area;
using AISpace.Common.Network.Packets.Common;
using Microsoft.Extensions.Logging;

namespace AISpace.Common.Network.Handlers;

public class AreasvEnterHandler(ILogger<AreasvEnterHandler> _logger, IUserSessionRepository _sessionRepo, SharedState state) : IPacketHandler
{
    public PacketType RequestType => PacketType.AreasvEnterRequest;
    public PacketType ResponseType => PacketType.AreasvEnterResponse;
    public MessageDomain Domain => MessageDomain.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, ClientConnection connection, CancellationToken ct = default)
    {
        var loginReq = AreasvEnterRequest.FromBytes(payload.Span);
        var session = await _sessionRepo.GetValidSessionAsync(loginReq.OTP, ct);

        if (session is null || session.UserId != loginReq.UserID) {
            await connection.SendAsync(ResponseType, new LoginResponse(AuthResponseResult.InvalidCredentials).ToBytes(), ct);
            return;
        }

        // 1. Привязываем ID
        connection.User = session.User;
        uint charId = (uint)connection.User.Characters.First().Id;
        connection.CharacterId = charId;

        // 2. Регистрируем в мире (SharedState теперь почистит старую сессию)
        state.RegisterClient("Area", connection);

        // 3. Отвечаем клиенту (Успех входа)
        await connection.SendAsync(ResponseType, new AreasvEnterResponse(0, charId).ToBytes(), ct);

        // 4. СИНХРОНИЗАЦИЯ: Чтобы все увидели всех
        _ = Task.Run(async () => {
            await Task.Delay(1500, ct); // Ждем прогрузку карты

            var cha = connection.User.Characters.First();
            var myPos = new MovementData(connection.X, connection.Y, connection.Z, connection.Rotation, MovementType.Stopped);
            
            // Спавним МЕНЯ у МЕНЯ (Result 0)
            await connection.SendAsync(PacketType.AvatarNotifyData, CreateNotify(cha, charId, 0, myPos), ct);

            foreach (var other in state.AreaClients.Values) {
                if (other.Id == connection.Id) continue;

                // Спавним МЕНЯ у ДРУГИХ
                await other.SendAsync(PacketType.AvatarNotifyData, CreateNotify(cha, charId, 1, myPos), ct);

                // Спавним ДРУГИХ у МЕНЯ
                var oCha = other.User?.Characters.FirstOrDefault();
                if (oCha != null) {
                    var oPos = new MovementData(other.X, other.Y, other.Z, other.Rotation, MovementType.Stopped);
                    await connection.SendAsync(PacketType.AvatarNotifyData, CreateNotify(oCha, other.CharacterId, 1, oPos), ct);
                }
            }
        }, ct);
    }

    static byte[] CreateNotify(DAL.Entities.Character cha, uint objId, uint res, MovementData pos) {
        var cd = new CharaData(objId, cha.ModelId, cha.Name) { moveData = pos };
        cd.Visual.VisualId = (uint)cha.Id;
        cd.Visual.BloodType = cha.BloodType;
        cd.Visual.Month = (byte)cha.Birthdate.Month;
        cd.Visual.Day = (byte)cha.Birthdate.Day;
        cd.Visual.Gender = (uint)cha.Gender;
        cd.Visual.Face = (byte)cha.FaceType;
        cd.Visual.Hairstyle = cha.Hairstyle;
        foreach (var eq in cha.Equipment) cd.AddEquip((uint)eq.ItemId, eq.SlotIndex);
        return new AvatarNotifyData(res, new AvatarData(objId, cd)).ToBytes();
    }
}