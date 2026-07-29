using AISpace.Common.DAL.Repositories;
using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Data;
using AISpace.Network.Packets.Area;

namespace AISpace.Common.Handlers.Area;

public class AreaEquipOrderListHandler(ICharacterRepository characterRepo)
    : IPacketHandler,
        IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.EquipOrderListRequest;

    public PacketType ResponseType => PacketType.EquipOrderListResponse;

    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var gender = 1;
        if (session.CharacterId != 0)
        {
            var cha = await characterRepo.GetByIdAsync((int)session.CharacterId, ct);
            if (cha is not null)
                gender = cha.Gender;
        }

        var response = new EquipOrderListResponse
        {
            CharaOrders = CharaOrderData.ForGender(gender),
        };
        await session.SendAsync(ResponseType, response.ToBytes(), ct);
    }
}
