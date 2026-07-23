using System.Buffers.Binary;
using AISpace.Network.Data;

namespace AISpace.Network.Tests;

public class RoboDataTests
{
    [Fact]
    public void RoboData_round_trips_the_complete_named_wire_layout()
    {
        var character = new CharaData(2_000_000_001, 1_002_011, "Robo")
        {
            JobId = 21,
            Progress = new LevelProgressData
            {
                Level = 12,
                StatusPoints = 13,
                Experience = 14,
                ExperienceToNextLevel = 15,
            },
        };
        character.AddEquip(1001, 2001);

        var robo = new RoboData(101, character, state: 2)
        {
            OwnerAvatarId = 202,
            ClientReserved = 0x10203040,
            AiScriptId = 303,
            EmotionId = 404,
            AvailableStatusPoints = 25,
            DistributedStatusPoints = [1, 2, 3, 4, 5],
            UserStatus = new UserStatusData { StatusText = "Robo status", StatusIconId = 6 },
        };
        robo.ItemUseEffects[0] = new ItemUseEffectData
        {
            ItemSerialId = 501,
            Enabled = 1,
            ItemDefinitionId = 502,
            EffectType = 7,
            Parameters = [601, 602, 603, 604, 605],
            OverwriteExisting = 1,
        };

        var bytes = robo.ToBytes();

        Assert.Equal(RoboData.WireSize, bytes.Length);
        Assert.Equal(101u, BinaryPrimitives.ReadUInt32LittleEndian(bytes));
        Assert.Equal(202u, BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(4)));
        Assert.Equal(2u, BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(8)));
        Assert.Equal(0x10203040u, BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(12)));
        Assert.Equal(303, BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(16)));

        var effectOffset = 4 * sizeof(uint) + sizeof(ushort) + CharaData.WireSize;
        Assert.Equal(501u, BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(effectOffset)));

        var roboTailOffset = effectOffset + RoboData.ItemUseEffectCount * ItemUseEffectData.WireSize;
        Assert.Equal(404u, BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(roboTailOffset)));
        Assert.Equal(25u, BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(roboTailOffset + sizeof(uint))));
        Assert.Equal(1u, BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(roboTailOffset + 2 * sizeof(uint))));
        Assert.Equal("Robo status", UserStatusData.FromBytes(bytes.AsSpan(roboTailOffset + 7 * sizeof(uint))).StatusText);

        var parsed = RoboData.FromBytes(bytes);
        Assert.Equal(101u, parsed.RoboId);
        Assert.Equal(202u, parsed.OwnerAvatarId);
        Assert.Equal(2u, parsed.State);
        Assert.Equal(0x10203040u, parsed.ClientReserved);
        Assert.Equal(303, parsed.AiScriptId);
        Assert.Equal(2_000_000_001u, parsed.Character.SlotId);
        Assert.Equal(21u, parsed.Character.JobId);
        Assert.Equal(12, parsed.Character.Progress.Level);
        Assert.Equal(501u, parsed.ItemUseEffects[0].ItemSerialId);
        Assert.Equal([601u, 602u, 603u, 604u, 605u], parsed.ItemUseEffects[0].Parameters);
        Assert.Equal(404u, parsed.EmotionId);
        Assert.Equal(25u, parsed.AvailableStatusPoints);
        Assert.Equal([1u, 2u, 3u, 4u, 5u], parsed.DistributedStatusPoints);
        Assert.Equal("Robo status", parsed.UserStatus.StatusText);
        Assert.Equal(6u, parsed.UserStatus.StatusIconId);
    }
}
