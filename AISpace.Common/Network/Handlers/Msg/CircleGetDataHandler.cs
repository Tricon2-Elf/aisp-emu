using AISpace.Common.DAL;
using AISpace.Common.Game;
using AISpace.Common.Network.Packets.Msg;
using Microsoft.EntityFrameworkCore;

namespace AISpace.Common.Network.Handlers.Msg;

public class CircleGetDataHandler(MainContext db) : IPacketHandler
{
    public PacketType RequestType => PacketType.CircleGetDataRequest;
    public PacketType ResponseType => PacketType.CircleGetDataResponse;
    public MessageDomain Domain => MessageDomain.Msg;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, ClientConnection connection, CancellationToken ct = default)
    {
        /*
        var list = new List<CircleData>();

        if (connection.User != null)
        {
            // Перезагружаем персонажа
            var cha = await db.Characters
                .Include(c => c.Circle)
                .FirstOrDefaultAsync(c => c.Id == connection.CharacterId, ct);

            if (cha != null && cha.Circle != null)
            {
                // Используем текущий ID персонажа как основной идентификатор
                uint myId = (uint)cha.Id;
                uint myCircleId = (uint)cha.Circle.Id;

                // 1. Добавляем круг в список
                list.Add(new CircleData(myCircleId, cha.Circle.Name, myId));

                // 2. Шлем состав (Себя как лидера)
                var membersList = new List<CircleMemberData>
                {
                    new CircleMemberData 
                    { 
                        AvatarId = myId, 
                        Name = cha.Name, 
                        Role = 2u // Лидер
                    }
                };

                var notify = new CircleNotifyMember(myCircleId, membersList);
                await connection.SendAsync(PacketType.CircleNotifyMember, notify.ToBytes(), ct);
            }
        }

        // 3. Отправляем финальный ответ
        var response = new CircleGetDataResponse(0, list);
        await connection.SendAsync(ResponseType, response.ToBytes(), ct);
        */
    }
}