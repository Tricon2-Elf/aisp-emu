using System.Buffers.Binary;
using AISpace.Network.Data;

namespace AISpace.Network.Tests;

public class AvatarDataTests
{
    [Fact]
    public void AvatarData_round_trips_the_complete_named_wire_layout()
    {
        var character = new CharaData(11, 22, "Avatar")
        {
            CharacterParameterId = 33,
            Map = new CharacterMapData
            {
                ChannelId = 44,
                MapId = 55,
                MapSerialId = 66,
                RouteState = 77,
                Movement = new MovementData(1.25f, 2.5f, 3.75f, 8, MovementType.Running),
            },
            TpsActionReferenceX = 4.25f,
            TpsActionReferenceY = 5.5f,
            ClientReserved = 88,
            JobId = 99,
            TpsActionProfileId = 111,
            CollisionRadius = 12.5f,
            TpsActionVerticalRange = 13.5f,
            Battle = new TpsBattleData
            {
                HitPoints = new HitPointData
                {
                    Current = 100,
                    BaseMaximum = 120,
                    MaximumBonus = 10,
                    MaximumPenalty = 5,
                    CurrentHearts = 3,
                    MaximumHearts = 4,
                },
                Stamina = new StaminaData
                {
                    Current = 14.5f,
                    RecoveryRate = 1.5f,
                    CostReductionBonus = 6,
                    CostReductionPenalty = 2,
                },
                Tank = new TankData
                {
                    Current = 30,
                    BaseMaximum = 40,
                    MaximumBonus = 8,
                    MaximumPenalty = 3,
                },
                BaseAbilities = new BattleAbilityValues { Values = [1, 2, 3, 4, 5] },
                AbilityModifierType0 = new BattleAbilityValues { Values = [6, 7, 8, 9, 10] },
                AbilityModifierType1 = new BattleAbilityValues { Values = [11, 12, 13, 14, 15] },
                AbilityModifierType2 = new BattleAbilityValues { Values = [16, 17, 18, 19, 20] },
                StatusEffectFlags = 0x1122334455667788,
                ActionFlags = 0x99AABBCC,
                ActiveSkillId = 1234,
                Cosplay = new CosplayProgressData
                {
                    CosplayId = 4321,
                    Progress = new LevelProgressData
                    {
                        Level = 7,
                        StatusPoints = 8,
                        Experience = 9,
                        ExperienceToNextLevel = 10,
                    },
                },
            },
            Progress = new LevelProgressData
            {
                Level = 12,
                StatusPoints = 13,
                Experience = 14,
                ExperienceToNextLevel = 15,
            },
        };

        character.AddEquip(1001, 2001);

        var avatar = new AvatarData(123, character)
        {
            ClientReserved = 0x10203040,
            EmotionId = 5678,
            RoboVoiceType = 3,
            UserStatus = new UserStatusData { StatusText = "Testing", StatusIconId = 6 },
        };
        avatar.ItemUseEffects[0] = new ItemUseEffectData
        {
            ItemSerialId = 201,
            Enabled = 1,
            ItemDefinitionId = 202,
            EffectType = 7,
            Parameters = [301, 302, 303, 304, 305],
            OverwriteExisting = 1,
        };

        var bytes = avatar.ToBytes();

        Assert.Equal(CharaData.WireSize, character.ToBytes().Length);
        Assert.Equal(AvatarData.WireSize, bytes.Length);
        Assert.Equal(123u, BinaryPrimitives.ReadUInt32LittleEndian(bytes));
        Assert.Equal(201u, BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(4 + CharaData.WireSize)));
        var avatarTailOffset = 4 + CharaData.WireSize + AvatarData.ItemUseEffectCount * ItemUseEffectData.WireSize;
        Assert.Equal(0x10203040u, BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(avatarTailOffset)));
        Assert.Equal(5678u, BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(avatarTailOffset + sizeof(uint))));
        Assert.Equal(3, bytes[avatarTailOffset + 2 * sizeof(uint)]);
        Assert.Equal("Testing", UserStatusData.FromBytes(bytes.AsSpan(avatarTailOffset + 2 * sizeof(uint) + sizeof(byte))).StatusText);

        var parsed = AvatarData.FromBytes(bytes);
        Assert.Equal(123u, parsed.AvatarId);
        Assert.Equal(55u, parsed.Character.Map.MapId);
        Assert.Equal(99u, parsed.Character.JobId);
        Assert.Equal(100u, parsed.Character.Battle.HitPoints.Current);
        Assert.Equal(14.5f, parsed.Character.Battle.Stamina.Current);
        Assert.Equal(30u, parsed.Character.Battle.Tank.Current);
        Assert.Equal([1u, 2u, 3u, 4u, 5u], parsed.Character.Battle.BaseAbilities.Values);
        Assert.Equal(0x1122334455667788ul, parsed.Character.Battle.StatusEffectFlags);
        Assert.Equal(1234u, parsed.Character.Battle.ActiveSkillId);
        Assert.Equal(4321u, parsed.Character.Battle.Cosplay.CosplayId);
        Assert.Equal(12, parsed.Character.Progress.Level);
        Assert.Equal(14ul, parsed.Character.Progress.Experience);
        Assert.Equal(201u, parsed.ItemUseEffects[0].ItemSerialId);
        Assert.Equal(7u, parsed.ItemUseEffects[0].EffectType);
        Assert.Equal([301u, 302u, 303u, 304u, 305u], parsed.ItemUseEffects[0].Parameters);
        Assert.Equal(5678u, parsed.EmotionId);
        Assert.Equal(3, parsed.RoboVoiceType);
        Assert.Equal("Testing", parsed.UserStatus.StatusText);
        Assert.Equal(6u, parsed.UserStatus.StatusIconId);
    }
}
