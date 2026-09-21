using aisp.Common.DAL.Entities;
using aisp.Common.DAL.Repositories;
using aisp.Network;
using aisp.Network.Data;
using aisp.Network.Packets.Area;
using Microsoft.Extensions.Logging;

namespace aisp.Common.Game;

/// <summary>
/// Shared TPS enter sequence: Charadoll swap, mission curtain/countdown, raise unlock, mob spawn.
/// </summary>
public static class TpsCombatEnter
{
    public static async Task<bool> TryEnterAsCharadollAsync(
        IPlayerSession session,
        IRoboRepository roboRepository,
        ILogger logger,
        CancellationToken ct = default
    )
    {
        if (session.IsTpsMode)
            return true;

        uint playerObjId = session.CharacterId;
        var myRobo = await roboRepository.GetAsync(checked((int)playerObjId), 1u, ct);
        if (myRobo is null)
        {
            logger.LogError(
                "TPS enter failed: Charadoll (robo 1) not found for character {CharacterId}",
                playerObjId
            );
            return false;
        }

        session.IsTpsMode = true;
        session.NeedsPostLoadSelfAvatarNotify = false;

        var myPos = new MovementData(
            session.X,
            session.Y,
            session.Z,
            session.Rotation,
            MovementType.Stopped
        );

        var doll = BuildControllableCharadoll(session, myRobo, myPos);

        // CreateItemEquipment picks CHandAttachEquipment only when CItemTable
        // Socket1 has 0x20000000. Login item_base may omit 12300010 or only
        // have the bag bit (SeedItemsIfEmpty). Upsert the catalog row first.
        await session.SendAsync(
            PacketType.NotifyItemBase,
            new NotifyItemBase(BuildWaterGunItemBase()).ToBytes(),
            ct
        );
        await CharacterItemSync.SendInventoryItemAsync(
            session,
            (int)TpsPrototypeConstants.WaterGunItemId,
            1,
            ct
        );

        await session.SendAsync(
            PacketType.NotifyDisappearChara,
            new NotifyDisappearChara(playerObjId).ToBytes(),
            ct
        );
        await Task.Delay(50, ct);

        await session.SendAsync(
            PacketType.AvatarNotifyData,
            new AvatarNotifyData(0, new AvatarData(playerObjId, doll)).ToBytes(),
            ct
        );

        // Mission curtain + start data — without these the black curtain never lifts / countdown never runs.
        await session.SendAsync(
            PacketType.NotifyMissionData,
            new NotifyMissionData().ToBytes(),
            ct
        );
        await session.SendAsync(
            PacketType.NotifyMissionStartData,
            new NotifyMissionStartData(
                leaderCharacterId: playerObjId,
                characterId: playerObjId,
                characterName: session.Character?.Name ?? "Player",
                timeLimitSeconds: TpsPrototypeConstants.MissionTimeLimitSeconds,
                targetCount: TpsPrototypeConstants.MissionTargetCount
            ).ToBytes(),
            ct
        );

        // Socket 0 lets CreateItemEquipment use catalog Socket1 (1<<19) for the
        // hand-attach class. A packet bit that is not in the catalog skips that path.
        await session.SendAsync(
            PacketType.NotifyBattleRaiseStart,
            new NotifyBattleRaiseStart(
                playerObjId,
                [new ItemEquipEntry(TpsPrototypeConstants.WaterGunItemId, 0)]
            ).ToBytes(),
            ct
        );
        await session.SendAsync(
            PacketType.RoboGetObtainedSkillListResponse,
            new GetObtainedSkillListResponse(0, 1, TpsPrototypeConstants.DefaultSkills).ToBytes(),
            ct
        );

        _ = Task.Run(
            async () =>
            {
                try
                {
                    await Task.Delay(2000, ct);

                    for (uint i = 4; i > 0; i--)
                    {
                        await session.SendAsync(
                            PacketType.NotifyMissionAction,
                            new NotifyMissionAction(5, i).ToBytes(),
                            ct
                        );
                        await Task.Delay(1000, ct);
                    }

                    await session.SendAsync(
                        PacketType.NotifyMissionAction,
                        new NotifyMissionAction(5, 0).ToBytes(),
                        ct
                    );
                    await session.SendAsync(
                        PacketType.NotifyMissionPartyStartOkUpdate,
                        new NotifyMissionPartyStartOkUpdate(playerObjId, 0).ToBytes(),
                        ct
                    );

                    // RaiseStart without RaiseEnd leaves the local TPS controller locked.
                    await session.SendAsync(
                        PacketType.NotifyBattleRaiseEnd,
                        new NotifyBattleRaiseEnd(playerObjId).ToBytes(),
                        ct
                    );
                    await session.SendAsync(
                        PacketType.EventEndNotify,
                        new EventEndNotify(0).ToBytes(),
                        ct
                    );
                    // Socket 0: SetItem uses catalog Socket1 (weapon + hand bits).
                    await session.SendAsync(
                        PacketType.ItemEquippedNotify,
                        new ItemEquippedNotify(
                            playerObjId,
                            TpsPrototypeConstants.WaterGunItemId,
                            0
                        ).ToBytes(),
                        ct
                    );

                    await Task.Delay(500, ct);
                    await SpawnPrototypeMobAsync(session, logger, ct);

                    await session.SendAsync(
                        PacketType.NotifyMissionAction,
                        new NotifyMissionAction(
                            0,
                            TpsPrototypeConstants.MissionTimeLimitSeconds
                        ).ToBytes(),
                        ct
                    );
                    await session.SendAsync(
                        PacketType.NotifyTimelimitShow,
                        new NotifyTimelimitShow(
                            0,
                            TpsPrototypeConstants.MissionTimeLimitSeconds
                        ).ToBytes(),
                        ct
                    );
                }
                catch (OperationCanceledException)
                {
                    // map leave / disconnect
                }
                catch (Exception ex)
                {
                    logger.LogError(
                        ex,
                        "TPS mission follow-up failed for character {CharacterId}",
                        playerObjId
                    );
                }
            },
            ct
        );

        return true;
    }

    public static CharaData BuildControllableCharadoll(
        IPlayerSession session,
        RoboData myRobo,
        MovementData myPos
    )
    {
        var doll = myRobo.Character;
        doll.SlotId = session.CharacterId;
        doll.Visual.VisualId = session.CharacterId;
        doll.Map = new CharacterMapData
        {
            ChannelId = checked((uint)session.ChannelId),
            MapId = session.MapId,
            MapSerialId = session.MapId,
            RouteState = 1,
            Movement = myPos,
        };
        doll.NamePlate = 0;
        doll.TpsActionProfileId = 1;
        doll.TpsActionReferenceX = 600f;
        doll.TpsActionReferenceY = 600f;
        doll.CollisionRadius = 10f;
        doll.TpsActionVerticalRange = 10f;

        ForceWaterGunEquip(doll);
        EnsureBattleDefaults(doll);

        doll.Battle.HitPoints.Current = doll.Battle.HitPoints.BaseMaximum;
        doll.Battle.Cosplay.CosplayId = 0;
        doll.Battle.ActionFlags = 1;
        doll.Battle.ActiveSkillId = TpsPrototypeConstants.DefaultSkills[0];
        return doll;
    }

    private static void ForceWaterGunEquip(CharaData doll)
    {
        while (doll.Equips.Count < CharaData.EquipmentSlotCount)
            doll.Equips.Add(new ItemSlotInfo(0, 0));

        for (var i = 0; i < doll.Equips.Count; i++)
        {
            if (doll.Equips[i].ItemId == TpsPrototypeConstants.WaterGunItemId)
                doll.Equips[i] = new ItemSlotInfo(0, 0);
        }

        doll.Equips[TpsPrototypeConstants.WaterGunEquipSlot] = new ItemSlotInfo(
            TpsPrototypeConstants.WaterGunItemId,
            0
        );
    }

    private static void EnsureBattleDefaults(CharaData doll)
    {
        if (doll.Battle.HitPoints.BaseMaximum == 0)
        {
            doll.Battle.HitPoints = new HitPointData
            {
                Current = TpsPrototypeConstants.DefaultHitPoints,
                BaseMaximum = TpsPrototypeConstants.DefaultHitPoints,
                CurrentHearts = TpsPrototypeConstants.DefaultHearts,
                MaximumHearts = TpsPrototypeConstants.DefaultHearts,
            };
        }

        if (doll.Battle.Tank.BaseMaximum == 0)
        {
            doll.Battle.Tank = new TankData
            {
                Current = TpsPrototypeConstants.DefaultTank,
                BaseMaximum = TpsPrototypeConstants.DefaultTank,
            };
        }

        if (doll.Battle.Stamina.Current <= 0)
            doll.Battle.Stamina = new StaminaData { Current = 100f, RecoveryRate = 10f };
    }

    private static async Task SpawnPrototypeMobAsync(
        IPlayerSession session,
        ILogger logger,
        CancellationToken ct
    )
    {
        uint mobObjId = TpsPrototypeConstants.MobObjectId;
        var mobSpawnPos = new MovementData(
            TpsPrototypeConstants.MobSpawnX,
            TpsPrototypeConstants.MobSpawnY,
            TpsPrototypeConstants.MobSpawnZ,
            TpsPrototypeConstants.MobSpawnRotation,
            MovementType.Stopped
        );

        var mobChara = new CharaData(mobObjId, TpsPrototypeConstants.MobModelId, "Shadow Cat")
        {
            Map = new CharacterMapData
            {
                ChannelId = checked((uint)session.ChannelId),
                MapId = session.MapId,
                MapSerialId = session.MapId,
                RouteState = 1,
                Movement = mobSpawnPos,
            },
            Visual = new CharaVisual(BloodType.A, 1, 1, 0, mobObjId, 0, 0),
            TpsActionReferenceX = 20f,
            TpsActionReferenceY = 20f,
            CollisionRadius = TpsPrototypeConstants.MobCollisionRadius,
            TpsActionVerticalRange = TpsPrototypeConstants.MobTpsActionVerticalRange,
            // Hostile filter: target action+12 >= 2 and != local (1).
            TpsActionProfileId = TpsPrototypeConstants.MobModelId,
            NamePlate = 1,
            Battle = new TpsBattleData
            {
                HitPoints = new HitPointData
                {
                    Current = TpsPrototypeConstants.DefaultHitPoints,
                    BaseMaximum = TpsPrototypeConstants.DefaultHitPoints,
                    CurrentHearts = TpsPrototypeConstants.DefaultHearts,
                    MaximumHearts = TpsPrototypeConstants.DefaultHearts,
                },
                Stamina = new StaminaData { Current = 100f, RecoveryRate = 10f },
                Tank = new TankData
                {
                    Current = TpsPrototypeConstants.DefaultTank,
                    BaseMaximum = TpsPrototypeConstants.DefaultTank,
                },
                ActionFlags = 1,
                BaseAbilities = new BattleAbilityValues { Values = [50, 50, 50, 50, 50] },
            },
        };

        mobChara.Equips.Clear();
        for (var i = 0; i < CharaData.EquipmentSlotCount; i++)
            mobChara.Equips.Add(new ItemSlotInfo(0, 0));

        TpsCombatTestState.ResetMonster(mobObjId);

        // NpcNotifyData instantiates the collidable CChara. InitChara127 then
        // deletes that slot and recreates it as controller type 128.
        await session.SendAsync(
            PacketType.NpcNotifyData,
            new NpcNotifyData(0, mobObjId, mobChara).ToBytes(),
            ct
        );

        // First MonsterData dword is not the slot id (that is Chara.SlotId).
        // uint at +628 is m_Type; default 1 selects the enemy controller path.
        var monsterData = new MonsterData(0, mobChara);
        await session.SendAsync(
            PacketType.NotifyMonsterData,
            new NotifyMonsterData(monsterData).ToBytes(),
            ct
        );
        await session.SendAsync(
            PacketType.NotifyShowChara,
            new NotifyShowChara(mobObjId, mobSpawnPos).ToBytes(),
            ct
        );

        logger.LogInformation(
            "TPS mob {MobObjId} spawned at ({X}, {Y}, {Z})",
            mobObjId,
            TpsPrototypeConstants.MobSpawnX,
            TpsPrototypeConstants.MobSpawnY,
            TpsPrototypeConstants.MobSpawnZ
        );
    }

    internal static ItemData BuildWaterGunItemBase() =>
        ItemEntityMapper.ToItemBaseListData(
            new Item
            {
                Id = (int)TpsPrototypeConstants.WaterGunItemId,
                Socket = 18,
                IconId = (int)TpsPrototypeConstants.WaterGunItemId,
                Name = "N/A",
            }
        );
}
