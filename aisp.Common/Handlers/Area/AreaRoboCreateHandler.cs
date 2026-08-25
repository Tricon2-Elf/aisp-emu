using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Data;
using aisp.Network.Packets.Area;
using Microsoft.Extensions.Logging;

namespace aisp.Common.Handlers.Area;

public class AreaRoboCreateHandler(
    IRoboRepository roboRepository,
    IWordFilter wordFilter,
    ILogger<AreaRoboCreateHandler> logger
) : IPacketHandler, IRequiresAuthenticatedSession
{
    private const uint DefaultHairItemId = 10930010;
    private const uint DefaultRoboId = 1;

    public PacketType RequestType => PacketType.RoboCreateRequest;
    public PacketType ResponseType => PacketType.RoboCreateResponse;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var characterId = checked((int)session.CharacterId);
        var request = RoboCreateRequest.FromBytes(payload.Span);
        // Client always calls the first doll as id 1 after create; keep ids stable.
        var roboId = DefaultRoboId;
        var objectId = RoboRepository.GetObjectId(session.CharacterId, roboId);

        logger.LogInformation(
            "RoboCreateRequest from character {CharacterId}: name={Name}, model={ModelId}, visual={Visual}, objectId={ObjectId}",
            session.CharacterId,
            request.Name,
            request.ModelId,
            request.Visual,
            objectId
        );

        if (wordFilter.ContainsBlockedWord(request.Name))
        {
            logger.LogWarning(
                "Rejecting Robo create for character {CharacterId}: blocked name",
                session.CharacterId
            );
            var stub = new RoboData(roboId, new CharaData(objectId, request.ModelId, string.Empty))
            {
                OwnerAvatarId = session.CharacterId,
            };
            await session.SendAsync(ResponseType, new RoboCreateResponse(1, stub).ToBytes(), ct);
            return;
        }

        // Never reuse CharacterId as m_SlotId — after call, client LookupChara(slotId) and may destroy it when state=0.
        var blood = (uint)request.Visual.BloodType is >= 1 and <= 4
            ? request.Visual.BloodType
            : BloodType.A;
        var month = request.Visual.Month is >= 1 and <= 12 ? request.Visual.Month : (byte)1;
        var day = request.Visual.Day is >= 1 and <= 28 ? request.Visual.Day : (byte)1;
        var visual = new CharaVisual(
            blood,
            month,
            day,
            request.Visual.Gender,
            objectId,
            request.Visual.Face,
            0
        );

        var chara = new CharaData(objectId, request.ModelId, request.Name) { Visual = visual };
        chara.AddEquip(
            DefaultClothingItems.Female.Select(
                (itemId, slot) => new CharacterEquipSlot((byte)slot, (uint)itemId)
            ),
            _ => 0
        );

        // The doll-making UI calls the newly created Robo after this response.
        var robo = new RoboData(roboId, chara, state: (uint)RoboState.Resting)
        {
            OwnerAvatarId = session.CharacterId,
        };
        await roboRepository.UpsertAsync(characterId, robo, ct);

        var response = new RoboCreateResponse(0, robo);
        await session.SendAsync(ResponseType, response.ToBytes(), ct);
    }
}
