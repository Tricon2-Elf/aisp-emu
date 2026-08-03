using AISpace.Common.DAL.Entities;
using AISpace.Common.Game;
using AISpace.Network.Data;
using Microsoft.EntityFrameworkCore;

namespace AISpace.Common.DAL.Repositories;

public interface IRoboRepository
{
    Task<bool> ExistsAsync(int characterId, uint roboId, CancellationToken ct = default);
    Task<RoboData?> GetAsync(int characterId, uint roboId, CancellationToken ct = default);
    Task<IReadOnlyList<RoboData>> GetAllAsync(int characterId, CancellationToken ct = default);
    Task<RoboEquipReplaceResult?> ReplaceEquipmentAsync(
        int characterId,
        uint roboId,
        IReadOnlyList<ItemEquipEntry> equips,
        CancellationToken ct = default
    );
    Task<bool> ReplaceDistributedStatusPointsAsync(
        int characterId,
        uint roboId,
        IReadOnlyList<uint> values,
        CancellationToken ct = default
    );
    Task UpsertAsync(int characterId, RoboData robo, CancellationToken ct = default);
}

public sealed class RoboRepository(MainContext db) : IRoboRepository
{
    private const uint ObjectIdBase = 2_000_000_000u;
    private const uint MaximumRobosPerCharacter = 10;
    private const uint MaximumClientObjectId = int.MaxValue;

    public static uint GetObjectId(uint ownerCharacterId, uint roboId)
    {
        if (roboId is 0 or > MaximumRobosPerCharacter)
            throw new ArgumentOutOfRangeException(
                nameof(roboId),
                roboId,
                $"Robo IDs must be between 1 and {MaximumRobosPerCharacter}."
            );

        var objectId = checked(
            ObjectIdBase + checked(ownerCharacterId * MaximumRobosPerCharacter) + roboId - 1
        );
        if (objectId > MaximumClientObjectId)
            throw new ArgumentOutOfRangeException(
                nameof(ownerCharacterId),
                ownerCharacterId,
                "Character ID is too large for the client object-ID namespace."
            );

        return objectId;
    }

    public static bool TryGetRoboId(uint ownerCharacterId, uint objectId, out uint roboId)
    {
        var firstObjectId =
            (ulong)ObjectIdBase + (ulong)ownerCharacterId * MaximumRobosPerCharacter;
        if (firstObjectId > MaximumClientObjectId || (ulong)objectId < firstObjectId)
        {
            roboId = 0;
            return false;
        }

        var offset = (ulong)objectId - firstObjectId;
        if (offset >= MaximumRobosPerCharacter)
        {
            roboId = 0;
            return false;
        }

        roboId = checked((uint)offset + 1);
        return true;
    }

    public Task<bool> ExistsAsync(int characterId, uint roboId, CancellationToken ct = default)
    {
        return db
            .Robos.AsNoTracking()
            .AnyAsync(x => x.CharacterId == characterId && x.RoboId == roboId, ct);
    }

    public async Task<RoboData?> GetAsync(
        int characterId,
        uint roboId,
        CancellationToken ct = default
    )
    {
        var entity = await WithDetails(db.Robos.AsNoTracking())
            .SingleOrDefaultAsync(x => x.CharacterId == characterId && x.RoboId == roboId, ct);
        return entity is null ? null : ToRoboData(entity);
    }

    public async Task<IReadOnlyList<RoboData>> GetAllAsync(
        int characterId,
        CancellationToken ct = default
    )
    {
        var entities = await WithDetails(db.Robos.AsNoTracking())
            .Where(x => x.CharacterId == characterId)
            .OrderBy(x => x.RoboId)
            .ToListAsync(ct);
        return entities.Select(ToRoboData).ToList();
    }

    public async Task<RoboEquipReplaceResult?> ReplaceEquipmentAsync(
        int characterId,
        uint roboId,
        IReadOnlyList<ItemEquipEntry> equips,
        CancellationToken ct = default
    )
    {
        if (equips.Count > CharaData.EquipmentSlotCount)
            throw new InvalidDataException(
                $"Robo equipment cannot contain more than {CharaData.EquipmentSlotCount} entries."
            );

        var entity = await WithDetails(db.Robos)
            .SingleOrDefaultAsync(x => x.CharacterId == characterId && x.RoboId == roboId, ct);
        if (entity is null)
            return null;

        var newBySlot = new Dictionary<byte, ItemEquipEntry>();
        foreach (var equip in equips)
        {
            if (
                equip.ItemId == 0
                || !EquipSlotMapper.TryResolveSlotIndex(
                    equip.ItemId,
                    equip.SocketBit,
                    out var slotIndex
                )
            )
                continue;

            var socket = ItemEntityMapper.ResolveBodyspot(equip.ItemId);
            newBySlot[slotIndex] = socket == 0 ? equip : new ItemEquipEntry(equip.ItemId, socket);
        }

        var removed = new List<EquippedItemChange>();
        var pendingAdds = new List<(byte SlotIndex, ItemEquipEntry Equip)>();

        foreach (var row in entity.Equipment)
        {
            if (row.ItemId == 0)
                continue;

            if (
                newBySlot.TryGetValue(row.SlotIndex, out var replacement)
                && replacement.ItemId == row.ItemId
            )
                continue;

            removed.Add(
                new EquippedItemChange(
                    (int)row.ItemId,
                    ItemName: null,
                    ItemEntityMapper.ResolveBodyspot(row.ItemId)
                )
            );
        }

        foreach (var (slotIndex, equip) in newBySlot)
        {
            var old = entity.Equipment.FirstOrDefault(e => e.SlotIndex == slotIndex);
            if (old is not null && old.ItemId == equip.ItemId)
                continue;

            pendingAdds.Add((slotIndex, equip));
        }

        var addedItemIds = pendingAdds.Select(x => (int)x.Equip.ItemId).Distinct().ToList();
        var addedItemsById = await db
            .Items.Where(i => addedItemIds.Contains(i.Id))
            .ToDictionaryAsync(i => i.Id, ct);

        var added = new List<EquippedItemChange>(pendingAdds.Count);
        foreach (var (_, equip) in pendingAdds)
        {
            var equipItemId = (int)equip.ItemId;
            addedItemsById.TryGetValue(equipItemId, out var item);
            var socket = ItemEntityMapper.ResolveBodyspot(equipItemId, name: item?.Name);
            if (socket == 0)
                socket = equip.SocketBit;
            added.Add(new EquippedItemChange(equipItemId, item?.Name, socket));
        }

        var changedItemIds = removed
            .Select(x => x.ItemId)
            .Concat(added.Select(x => x.ItemId))
            .Distinct()
            .ToList();
        var inventoryByItemId = await db
            .CharacterInventories.Where(i =>
                i.CharacterId == characterId && changedItemIds.Contains(i.ItemId)
            )
            .ToDictionaryAsync(i => i.ItemId, ct);

        // Newly equipped pieces must come from inventory (or from pieces removed from this
        // Robo in the same replace). Unequipped pieces return to the owner's inventory.
        var availableByItemId = inventoryByItemId.ToDictionary(x => x.Key, x => x.Value.Quantity);
        foreach (var change in removed)
            availableByItemId[change.ItemId] =
                availableByItemId.GetValueOrDefault(change.ItemId) + 1;

        var requiredByItemId = pendingAdds
            .GroupBy(x => (int)x.Equip.ItemId)
            .ToDictionary(g => g.Key, g => g.Count());

        foreach (var (itemId, required) in requiredByItemId)
        {
            if (availableByItemId.GetValueOrDefault(itemId) < required)
                throw new InvalidOperationException(
                    $"Character {characterId} does not own required quantity of item {itemId} for Robo {roboId}."
                );
        }

        foreach (var change in removed)
        {
            if (inventoryByItemId.TryGetValue(change.ItemId, out var existingInventory))
            {
                existingInventory.Quantity += 1;
            }
            else
            {
                existingInventory = new CharacterInventory
                {
                    CharacterId = characterId,
                    ItemId = change.ItemId,
                    Quantity = 1,
                };
                db.CharacterInventories.Add(existingInventory);
                inventoryByItemId[change.ItemId] = existingInventory;
            }
        }

        foreach (var (itemId, required) in requiredByItemId)
        {
            var inventory = inventoryByItemId[itemId];
            inventory.Quantity -= required;
            if (inventory.Quantity <= 0)
            {
                db.CharacterInventories.Remove(inventory);
                inventoryByItemId.Remove(itemId);
            }
        }

        foreach (var row in entity.Equipment)
        {
            if (newBySlot.TryGetValue(row.SlotIndex, out var equip))
            {
                row.ItemId = equip.ItemId;
                row.Socket = equip.SocketBit;
            }
            else
            {
                row.ItemId = 0;
                row.Socket = 0;
            }
        }

        entity.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        var countsByItemId = await db
            .CharacterInventories.Where(i =>
                i.CharacterId == characterId && changedItemIds.Contains(i.ItemId)
            )
            .ToDictionaryAsync(i => i.ItemId, i => i.Quantity, ct);

        foreach (var itemId in changedItemIds)
        {
            if (!countsByItemId.ContainsKey(itemId))
                countsByItemId[itemId] = 0;
        }

        return new RoboEquipReplaceResult(
            ToRoboData(entity),
            new EquipReplaceResult(removed, added, countsByItemId)
        );
    }

    public async Task<bool> ReplaceDistributedStatusPointsAsync(
        int characterId,
        uint roboId,
        IReadOnlyList<uint> values,
        CancellationToken ct = default
    )
    {
        if (values.Count != RoboData.DistributedStatusPointCount)
            throw new ArgumentException(
                $"Exactly {RoboData.DistributedStatusPointCount} distributed status-point values are required.",
                nameof(values)
            );

        var entity = await db
            .Robos.Include(x => x.DistributedStatusPoints)
            .SingleOrDefaultAsync(x => x.CharacterId == characterId && x.RoboId == roboId, ct);
        if (entity is null)
            return false;

        var previouslyDistributed = entity.DistributedStatusPoints.Aggregate(
            0UL,
            (total, point) => total + point.Value
        );
        var newlyDistributed = values.Aggregate(0UL, (total, value) => total + value);
        var totalBudget = (ulong)entity.AvailableStatusPoints + previouslyDistributed;
        if (newlyDistributed > totalBudget)
            return false;

        entity.AvailableStatusPoints = checked((uint)(totalBudget - newlyDistributed));
        for (byte index = 0; index < RoboData.DistributedStatusPointCount; index++)
        {
            var row = entity.DistributedStatusPoints.Single(x => x.StatusIndex == index);
            row.Value = values[index];
        }

        entity.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task UpsertAsync(int characterId, RoboData robo, CancellationToken ct = default)
    {
        Validate(characterId, robo);

        var entity = await WithDetails(db.Robos)
            .SingleOrDefaultAsync(x => x.CharacterId == characterId && x.RoboId == robo.RoboId, ct);
        var now = DateTime.UtcNow;
        if (entity is null)
        {
            entity = new Robo
            {
                CharacterId = characterId,
                RoboId = robo.RoboId,
                CreatedAt = now,
                TpsBattleData = new RoboTpsBattleData
                {
                    CharacterId = characterId,
                    RoboId = robo.RoboId,
                },
            };
            db.Robos.Add(entity);
        }

        CopyScalarData(entity, robo);
        SynchronizeEquipment(entity, robo.Character.Equips);
        SynchronizeItemUseEffects(entity, robo.ItemUseEffects);
        SynchronizeBattleAbilities(entity.TpsBattleData, robo.Character.Battle);
        SynchronizeDistributedStatusPoints(entity, robo.DistributedStatusPoints);
        entity.UpdatedAt = now;

        await db.SaveChangesAsync(ct);
    }

    private static IQueryable<Robo> WithDetails(IQueryable<Robo> query)
    {
        return query
            .Include(x => x.TpsBattleData)
                .ThenInclude(x => x.BattleAbilities)
            .Include(x => x.Equipment)
            .Include(x => x.ItemUseEffects)
            .Include(x => x.DistributedStatusPoints);
    }

    private static void Validate(int characterId, RoboData robo)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(characterId);
        if (robo.Character.Equips.Count > CharaData.EquipmentSlotCount)
            throw new InvalidOperationException(
                $"RoboData cannot contain more than {CharaData.EquipmentSlotCount} equipment slots."
            );
        if (robo.ItemUseEffects.Length != RoboData.ItemUseEffectCount)
            throw new InvalidOperationException(
                $"RoboData must contain exactly {RoboData.ItemUseEffectCount} item-use effects."
            );
        if (robo.ItemUseEffects.Any(x => x.Parameters.Length != ItemUseEffectData.ParameterCount))
            throw new InvalidOperationException(
                $"Each ItemUseEffectData must contain exactly {ItemUseEffectData.ParameterCount} parameters."
            );
        if (robo.DistributedStatusPoints.Length != RoboData.DistributedStatusPointCount)
            throw new InvalidOperationException(
                $"RoboData must contain exactly {RoboData.DistributedStatusPointCount} distributed status-point values."
            );

        ValidateAbilityValues(robo.Character.Battle.BaseAbilities);
        ValidateAbilityValues(robo.Character.Battle.AbilityModifierType0);
        ValidateAbilityValues(robo.Character.Battle.AbilityModifierType1);
        ValidateAbilityValues(robo.Character.Battle.AbilityModifierType2);
    }

    private static void ValidateAbilityValues(BattleAbilityValues abilities)
    {
        if (abilities.Values.Length != BattleAbilityValues.Count)
            throw new InvalidOperationException(
                $"BattleAbilityValues must contain exactly {BattleAbilityValues.Count} values."
            );
    }

    private static void CopyScalarData(Robo entity, RoboData source)
    {
        var character = source.Character;
        var visual = character.Visual;
        var battle = character.Battle;
        var hitPoints = battle.HitPoints;
        var stamina = battle.Stamina;
        var tank = battle.Tank;
        var cosplay = battle.Cosplay;
        var progress = character.Progress;
        var tpsBattleData = entity.TpsBattleData;

        entity.State = source.State;
        entity.AiScriptId = source.AiScriptId;

        entity.ModelId = character.ModelId;
        entity.Name = character.Name;
        entity.BloodType = visual.BloodType;
        entity.BirthMonth = visual.Month;
        entity.BirthDay = visual.Day;
        entity.Gender = visual.Gender;
        entity.Face = visual.Face;
        entity.Hairstyle = visual.Hairstyle;
        entity.ParameterId = character.CharacterParameterId;

        entity.JobId = character.JobId;

        tpsBattleData.ActionReferenceX = character.TpsActionReferenceX;
        tpsBattleData.ActionReferenceY = character.TpsActionReferenceY;
        tpsBattleData.ActionProfileId = character.TpsActionProfileId;
        tpsBattleData.CollisionRadius = character.CollisionRadius;
        tpsBattleData.ActionVerticalRange = character.TpsActionVerticalRange;

        tpsBattleData.HitPointsCurrent = hitPoints.Current;
        tpsBattleData.HitPointsBaseMaximum = hitPoints.BaseMaximum;
        tpsBattleData.HitPointsMaximumBonus = hitPoints.MaximumBonus;
        tpsBattleData.HitPointsMaximumPenalty = hitPoints.MaximumPenalty;
        tpsBattleData.CurrentHearts = hitPoints.CurrentHearts;
        tpsBattleData.MaximumHearts = hitPoints.MaximumHearts;
        tpsBattleData.StaminaCurrent = stamina.Current;
        tpsBattleData.StaminaRecoveryRate = stamina.RecoveryRate;
        tpsBattleData.StaminaCostReductionBonus = stamina.CostReductionBonus;
        tpsBattleData.StaminaCostReductionPenalty = stamina.CostReductionPenalty;
        tpsBattleData.TankCurrent = tank.Current;
        tpsBattleData.TankBaseMaximum = tank.BaseMaximum;
        tpsBattleData.TankMaximumBonus = tank.MaximumBonus;
        tpsBattleData.TankMaximumPenalty = tank.MaximumPenalty;
        tpsBattleData.StatusEffectFlags = battle.StatusEffectFlags;
        tpsBattleData.ActionFlags = battle.ActionFlags;
        tpsBattleData.ActiveSkillId = battle.ActiveSkillId;

        tpsBattleData.CosplayId = cosplay.CosplayId;
        tpsBattleData.CosplayLevel = cosplay.Progress.Level;
        tpsBattleData.CosplayStatusPoints = cosplay.Progress.StatusPoints;
        tpsBattleData.CosplayExperience = cosplay.Progress.Experience;
        tpsBattleData.CosplayExperienceToNextLevel = cosplay.Progress.ExperienceToNextLevel;

        entity.Level = progress.Level;
        entity.StatusPoints = progress.StatusPoints;
        entity.Experience = progress.Experience;
        entity.ExperienceToNextLevel = progress.ExperienceToNextLevel;

        entity.AvailableStatusPoints = source.AvailableStatusPoints;
        entity.UserStatusText = source.UserStatus.StatusText;
        entity.UserStatusIconId = source.UserStatus.StatusIconId;
    }

    private static void SynchronizeEquipment(Robo entity, IReadOnlyList<ItemSlotInfo> source)
    {
        for (byte slotIndex = 0; slotIndex < CharaData.EquipmentSlotCount; slotIndex++)
        {
            var item = slotIndex < source.Count ? source[slotIndex] : new ItemSlotInfo(0, 0);
            var row = entity.Equipment.SingleOrDefault(x => x.SlotIndex == slotIndex);
            if (row is null)
            {
                row = new RoboEquipment
                {
                    CharacterId = entity.CharacterId,
                    RoboId = entity.RoboId,
                    SlotIndex = slotIndex,
                };
                entity.Equipment.Add(row);
            }

            row.ItemId = item.ItemId;
            row.Socket = item.Socket;
        }
    }

    private static void SynchronizeItemUseEffects(
        Robo entity,
        IReadOnlyList<ItemUseEffectData> source
    )
    {
        for (byte slotIndex = 0; slotIndex < RoboData.ItemUseEffectCount; slotIndex++)
        {
            var effect = source[slotIndex];
            var row = entity.ItemUseEffects.SingleOrDefault(x => x.SlotIndex == slotIndex);
            if (row is null)
            {
                row = new RoboItemUseEffect
                {
                    CharacterId = entity.CharacterId,
                    RoboId = entity.RoboId,
                    SlotIndex = slotIndex,
                };
                entity.ItemUseEffects.Add(row);
            }

            row.ItemSerialId = effect.ItemSerialId;
            row.Enabled = effect.Enabled;
            row.ItemDefinitionId = effect.ItemDefinitionId;
            row.EffectType = effect.EffectType;
            row.Parameter0 = effect.Parameters[0];
            row.Parameter1 = effect.Parameters[1];
            row.Parameter2 = effect.Parameters[2];
            row.Parameter3 = effect.Parameters[3];
            row.Parameter4 = effect.Parameters[4];
            row.OverwriteExisting = effect.OverwriteExisting;
        }
    }

    private static void SynchronizeBattleAbilities(
        RoboTpsBattleData tpsBattleData,
        TpsBattleData battle
    )
    {
        SynchronizeBattleAbilitySet(
            tpsBattleData,
            RoboBattleAbilitySet.Base,
            battle.BaseAbilities.Values
        );
        SynchronizeBattleAbilitySet(
            tpsBattleData,
            RoboBattleAbilitySet.ModifierType0,
            battle.AbilityModifierType0.Values
        );
        SynchronizeBattleAbilitySet(
            tpsBattleData,
            RoboBattleAbilitySet.ModifierType1,
            battle.AbilityModifierType1.Values
        );
        SynchronizeBattleAbilitySet(
            tpsBattleData,
            RoboBattleAbilitySet.ModifierType2,
            battle.AbilityModifierType2.Values
        );
    }

    private static void SynchronizeBattleAbilitySet(
        RoboTpsBattleData tpsBattleData,
        RoboBattleAbilitySet abilitySet,
        IReadOnlyList<uint> source
    )
    {
        for (byte abilityIndex = 0; abilityIndex < BattleAbilityValues.Count; abilityIndex++)
        {
            var row = tpsBattleData.BattleAbilities.SingleOrDefault(x =>
                x.AbilitySet == abilitySet && x.AbilityIndex == abilityIndex
            );
            if (row is null)
            {
                row = new RoboBattleAbility
                {
                    CharacterId = tpsBattleData.CharacterId,
                    RoboId = tpsBattleData.RoboId,
                    AbilitySet = abilitySet,
                    AbilityIndex = abilityIndex,
                };
                tpsBattleData.BattleAbilities.Add(row);
            }

            row.Value = source[abilityIndex];
        }
    }

    private static void SynchronizeDistributedStatusPoints(Robo entity, IReadOnlyList<uint> source)
    {
        for (
            byte statusIndex = 0;
            statusIndex < RoboData.DistributedStatusPointCount;
            statusIndex++
        )
        {
            var row = entity.DistributedStatusPoints.SingleOrDefault(x =>
                x.StatusIndex == statusIndex
            );
            if (row is null)
            {
                row = new RoboDistributedStatusPoint
                {
                    CharacterId = entity.CharacterId,
                    RoboId = entity.RoboId,
                    StatusIndex = statusIndex,
                };
                entity.DistributedStatusPoints.Add(row);
            }

            row.Value = source[statusIndex];
        }
    }

    private static RoboData ToRoboData(Robo entity)
    {
        ValidateStoredCollections(entity);
        var tpsBattleData = entity.TpsBattleData;
        var objectId = GetObjectId(checked((uint)entity.CharacterId), entity.RoboId);

        var character = new CharaData(objectId, entity.ModelId, entity.Name)
        {
            Visual = new CharaVisual(
                entity.BloodType,
                entity.BirthMonth,
                entity.BirthDay,
                entity.Gender,
                objectId,
                entity.Face,
                entity.Hairstyle
            ),
            CharacterParameterId = entity.ParameterId,
            TpsActionReferenceX = tpsBattleData.ActionReferenceX,
            TpsActionReferenceY = tpsBattleData.ActionReferenceY,
            JobId = entity.JobId,
            TpsActionProfileId = tpsBattleData.ActionProfileId,
            CollisionRadius = tpsBattleData.CollisionRadius,
            TpsActionVerticalRange = tpsBattleData.ActionVerticalRange,
            Battle = new TpsBattleData
            {
                HitPoints = new HitPointData
                {
                    Current = tpsBattleData.HitPointsCurrent,
                    BaseMaximum = tpsBattleData.HitPointsBaseMaximum,
                    MaximumBonus = tpsBattleData.HitPointsMaximumBonus,
                    MaximumPenalty = tpsBattleData.HitPointsMaximumPenalty,
                    CurrentHearts = tpsBattleData.CurrentHearts,
                    MaximumHearts = tpsBattleData.MaximumHearts,
                },
                Stamina = new StaminaData
                {
                    Current = tpsBattleData.StaminaCurrent,
                    RecoveryRate = tpsBattleData.StaminaRecoveryRate,
                    CostReductionBonus = tpsBattleData.StaminaCostReductionBonus,
                    CostReductionPenalty = tpsBattleData.StaminaCostReductionPenalty,
                },
                Tank = new TankData
                {
                    Current = tpsBattleData.TankCurrent,
                    BaseMaximum = tpsBattleData.TankBaseMaximum,
                    MaximumBonus = tpsBattleData.TankMaximumBonus,
                    MaximumPenalty = tpsBattleData.TankMaximumPenalty,
                },
                BaseAbilities = ToBattleAbilityValues(tpsBattleData, RoboBattleAbilitySet.Base),
                AbilityModifierType0 = ToBattleAbilityValues(
                    tpsBattleData,
                    RoboBattleAbilitySet.ModifierType0
                ),
                AbilityModifierType1 = ToBattleAbilityValues(
                    tpsBattleData,
                    RoboBattleAbilitySet.ModifierType1
                ),
                AbilityModifierType2 = ToBattleAbilityValues(
                    tpsBattleData,
                    RoboBattleAbilitySet.ModifierType2
                ),
                StatusEffectFlags = tpsBattleData.StatusEffectFlags,
                ActionFlags = tpsBattleData.ActionFlags,
                ActiveSkillId = tpsBattleData.ActiveSkillId,
                Cosplay = new CosplayProgressData
                {
                    CosplayId = tpsBattleData.CosplayId,
                    Progress = new LevelProgressData
                    {
                        Level = tpsBattleData.CosplayLevel,
                        StatusPoints = tpsBattleData.CosplayStatusPoints,
                        Experience = tpsBattleData.CosplayExperience,
                        ExperienceToNextLevel = tpsBattleData.CosplayExperienceToNextLevel,
                    },
                },
            },
            Progress = new LevelProgressData
            {
                Level = entity.Level,
                StatusPoints = entity.StatusPoints,
                Experience = entity.Experience,
                ExperienceToNextLevel = entity.ExperienceToNextLevel,
            },
        };

        foreach (var equipment in entity.Equipment.OrderBy(x => x.SlotIndex))
            character.Equips.Add(new ItemSlotInfo(equipment.ItemId, equipment.Socket));

        var itemUseEffects = entity
            .ItemUseEffects.OrderBy(x => x.SlotIndex)
            .Select(x => new ItemUseEffectData
            {
                ItemSerialId = x.ItemSerialId,
                Enabled = x.Enabled,
                ItemDefinitionId = x.ItemDefinitionId,
                EffectType = x.EffectType,
                Parameters = [x.Parameter0, x.Parameter1, x.Parameter2, x.Parameter3, x.Parameter4],
                OverwriteExisting = x.OverwriteExisting,
            })
            .ToArray();

        return new RoboData(entity.RoboId, character, entity.State)
        {
            OwnerAvatarId = checked((uint)entity.CharacterId),
            AiScriptId = entity.AiScriptId,
            ItemUseEffects = itemUseEffects,
            AvailableStatusPoints = entity.AvailableStatusPoints,
            DistributedStatusPoints = entity
                .DistributedStatusPoints.OrderBy(x => x.StatusIndex)
                .Select(x => x.Value)
                .ToArray(),
            UserStatus = new UserStatusData
            {
                StatusText = entity.UserStatusText,
                StatusIconId = entity.UserStatusIconId,
            },
        };
    }

    private static BattleAbilityValues ToBattleAbilityValues(
        RoboTpsBattleData tpsBattleData,
        RoboBattleAbilitySet abilitySet
    )
    {
        return new BattleAbilityValues
        {
            Values = tpsBattleData
                .BattleAbilities.Where(x => x.AbilitySet == abilitySet)
                .OrderBy(x => x.AbilityIndex)
                .Select(x => x.Value)
                .ToArray(),
        };
    }

    private static void ValidateStoredCollections(Robo entity)
    {
        if (
            entity.Equipment.Count != CharaData.EquipmentSlotCount
            || entity.Equipment.Any(x => x.SlotIndex >= CharaData.EquipmentSlotCount)
        )
            throw InvalidStoredData(entity, $"{entity.Equipment.Count} equipment rows");
        if (
            entity.ItemUseEffects.Count != RoboData.ItemUseEffectCount
            || entity.ItemUseEffects.Any(x => x.SlotIndex >= RoboData.ItemUseEffectCount)
        )
            throw InvalidStoredData(entity, $"{entity.ItemUseEffects.Count} item-use effect rows");
        if (
            entity.DistributedStatusPoints.Count != RoboData.DistributedStatusPointCount
            || entity.DistributedStatusPoints.Any(x =>
                x.StatusIndex >= RoboData.DistributedStatusPointCount
            )
        )
            throw InvalidStoredData(
                entity,
                $"{entity.DistributedStatusPoints.Count} distributed status-point rows"
            );
        if (entity.TpsBattleData is null)
            throw InvalidStoredData(entity, "no TPS battle-data row");

        var validSets = Enum.GetValues<RoboBattleAbilitySet>();
        var expectedAbilityCount = validSets.Length * BattleAbilityValues.Count;
        if (
            entity.TpsBattleData.BattleAbilities.Count != expectedAbilityCount
            || entity.TpsBattleData.BattleAbilities.Any(x =>
                !validSets.Contains(x.AbilitySet) || x.AbilityIndex >= BattleAbilityValues.Count
            )
        )
            throw InvalidStoredData(
                entity,
                $"{entity.TpsBattleData.BattleAbilities.Count} battle-ability rows"
            );
    }

    private static InvalidDataException InvalidStoredData(Robo entity, string detail)
    {
        return new InvalidDataException(
            $"Stored Robo {entity.RoboId} for character {entity.CharacterId} has an invalid fixed collection: {detail}."
        );
    }
}
