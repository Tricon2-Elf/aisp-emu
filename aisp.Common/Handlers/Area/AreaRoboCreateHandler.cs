using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Data;
using aisp.Network.Packets.Area;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace aisp.Common.Handlers.Area;

public class AreaRoboCreateHandler(
    IRoboRepository roboRepository,
    ICharacterRepository characterRepository,
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

        if (wordFilter.ContainsBlockedWord(WordFilterLevel.Complete, request.Name))
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

        // Water gun for TPS combat prototype.
        while (chara.Equips.Count <= TpsPrototypeConstants.WaterGunEquipSlot)
            chara.AddEquip(0, 0);
        chara.Equips[TpsPrototypeConstants.WaterGunEquipSlot] = new ItemSlotInfo(
            TpsPrototypeConstants.WaterGunItemId,
            TpsPrototypeConstants.WaterGunSocketBit
        );

        chara.Battle = CreateDefaultTpsBattleData();

        // The doll-making UI calls the newly created Robo after this response.
        var robo = new RoboData(roboId, chara, state: (uint)RoboState.Resting)
        {
            OwnerAvatarId = session.CharacterId,
        };
        await roboRepository.UpsertAsync(characterId, robo, ct);

        try
        {
            await characterRepository.AddInventoryAsync(
                characterId,
                (int)TpsPrototypeConstants.WaterGunItemId,
                1,
                ct
            );
            await CharacterItemSync.SendInventoryItemAsync(
                session,
                (int)TpsPrototypeConstants.WaterGunItemId,
                1,
                ct
            );
        }
        catch (DbUpdateException ex)
        {
            // Prototype water gun may be absent from the item catalog in empty/test DBs.
            logger.LogWarning(
                ex,
                "Could not add water gun {ItemId} to inventory for character {CharacterId}",
                TpsPrototypeConstants.WaterGunItemId,
                characterId
            );
        }

        var response = new RoboCreateResponse(0, robo);
        await session.SendAsync(ResponseType, response.ToBytes(), ct);
    }

    private static TpsBattleData CreateDefaultTpsBattleData() =>
        new()
        {
            HitPoints = new HitPointData
            {
                Current = TpsPrototypeConstants.DefaultHitPoints,
                BaseMaximum = TpsPrototypeConstants.DefaultHitPoints,
                MaximumHearts = 5,
                CurrentHearts = 5,
            },
            Stamina = new StaminaData { Current = 100f, RecoveryRate = 10f },
            Tank = new TankData
            {
                Current = TpsPrototypeConstants.DefaultTank,
                BaseMaximum = TpsPrototypeConstants.DefaultTank,
            },
            BaseAbilities = new BattleAbilityValues { Values = [50, 50, 50, 50, 50] },
            AbilityModifierType0 = new BattleAbilityValues
            {
                Values = [200090, 200000, 200020, 0, 0],
            },
        };
}
