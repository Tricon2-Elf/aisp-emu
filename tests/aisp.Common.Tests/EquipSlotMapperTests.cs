using aisp.Common.Game;

namespace aisp.Common.Tests;

public class EquipSlotMapperTests
{
    [Theory]
    [InlineData(10800000u, 11u, (byte)CharacterEquipmentSlotIndex.Glasses)]
    [InlineData(
        10800000u,
        (uint)WardrobeSocketBit.Glasses,
        (byte)CharacterEquipmentSlotIndex.Glasses
    )]
    [InlineData(10800080u, 15u, (byte)CharacterEquipmentSlotIndex.LeftEarring)]
    [InlineData(
        10800080u,
        (uint)WardrobeSocketBit.LeftEarring,
        (byte)CharacterEquipmentSlotIndex.LeftEarring
    )]
    [InlineData(10800150u, 16u, (byte)CharacterEquipmentSlotIndex.RightEarring)]
    [InlineData(
        10800150u,
        (uint)WardrobeSocketBit.RightEarring,
        (byte)CharacterEquipmentSlotIndex.RightEarring
    )]
    [InlineData(10899999u, 23u, (byte)CharacterEquipmentSlotIndex.WristCharm)]
    [InlineData(11600060u, 12u, (byte)CharacterEquipmentSlotIndex.Necklace)]
    [InlineData(11600010u, 0u, (byte)CharacterEquipmentSlotIndex.Necklace)]
    [InlineData(11700020u, 14u, (byte)CharacterEquipmentSlotIndex.HairRibbon)]
    [InlineData(11700030u, 10u, (byte)CharacterEquipmentSlotIndex.Headband)]
    [InlineData(11800030u, 11u, (byte)CharacterEquipmentSlotIndex.Glasses)]
    [InlineData(11200000u, 0u, (byte)CharacterEquipmentSlotIndex.RightHandbag)]
    [InlineData(11200000u, 26u, (byte)CharacterEquipmentSlotIndex.LeftShoulderBag)]
    [InlineData(11400001u, 26u, (byte)CharacterEquipmentSlotIndex.Wings)]
    [InlineData(11400013u, 0u, (byte)CharacterEquipmentSlotIndex.Wings)]
    [InlineData(11400150u, 26u, (byte)CharacterEquipmentSlotIndex.Wings)]
    [InlineData(11400020u, 26u, (byte)CharacterEquipmentSlotIndex.LeftShoulderBag)]
    [InlineData(
        11400020u,
        (uint)WardrobeSocketBit.Wings,
        (byte)CharacterEquipmentSlotIndex.LeftShoulderBag
    )]
    [InlineData(11400070u, 27u, (byte)CharacterEquipmentSlotIndex.Tail)]
    [InlineData(11400070u, (uint)WardrobeSocketBit.Wings, (byte)CharacterEquipmentSlotIndex.Tail)]
    [InlineData(11400110u, 0u, (byte)CharacterEquipmentSlotIndex.Tail)]
    [InlineData(12200000u, 60u, (byte)CharacterEquipmentSlotIndex.Handheld)]
    [InlineData(10900000u, 51u, (byte)CharacterEquipmentSlotIndex.Wig)]
    [InlineData(10900000u, (uint)WardrobeSocketBit.Wig, (byte)CharacterEquipmentSlotIndex.Wig)]
    [InlineData(10930050u, 11u, (byte)CharacterEquipmentSlotIndex.Wig)]
    public void TryResolveSlotIndex_uses_seed_socket_as_window_slot(
        uint itemId,
        uint socketBit,
        byte expectedSlot
    )
    {
        Assert.True(EquipSlotMapper.TryResolveSlotIndex(itemId, socketBit, out var slotIndex));
        Assert.Equal(expectedSlot, slotIndex);
    }

    [Fact]
    public void TryResolveSlotIndex_does_not_put_earrings_or_wigs_in_the_wrong_window_cell()
    {
        Assert.True(EquipSlotMapper.TryResolveSlotIndex(10900000, 51, out var wig));
        Assert.True(
            EquipSlotMapper.TryResolveSlotIndex(10900000, 26, out var wigFromBackpackSocket)
        );
        Assert.True(EquipSlotMapper.TryResolveSlotIndex(10800000, 11, out var glasses));
        Assert.True(EquipSlotMapper.TryResolveSlotIndex(10800080, 15, out var leftEarring));
        Assert.True(EquipSlotMapper.TryResolveSlotIndex(10800150, 16, out var rightEarring));
        Assert.True(EquipSlotMapper.TryResolveSlotIndex(11600060, 12, out var necklace));
        Assert.True(EquipSlotMapper.TryResolveSlotIndex(11600010, 0, out var necktie));
        Assert.True(EquipSlotMapper.TryResolveSlotIndex(11700020, 14, out var hairRibbon));
        Assert.True(EquipSlotMapper.TryResolveSlotIndex(11800030, 11, out var mask));
        Assert.True(EquipSlotMapper.TryResolveSlotIndex(11200000, 0, out var bag));
        Assert.True(EquipSlotMapper.TryResolveSlotIndex(11400020, 26, out var backpack));
        Assert.True(EquipSlotMapper.TryResolveSlotIndex(11400001, 26, out var wings));
        Assert.True(EquipSlotMapper.TryResolveSlotIndex(11400150, 26, out var wingsFrom150));
        Assert.True(EquipSlotMapper.TryResolveSlotIndex(11400070, 26, out var tail));
        Assert.True(EquipSlotMapper.TryResolveSlotIndex(12200000, 60, out var handheld));

        Assert.Equal(wig, wigFromBackpackSocket);
        Assert.Equal(14, necklace);
        Assert.Equal(necklace, necktie);
        Assert.Equal(16, hairRibbon);
        Assert.Equal(12, mask);
        Assert.Equal(19, bag);
        Assert.Equal(26, backpack);
        Assert.Equal(27, wings);
        Assert.Equal(wings, wingsFrom150);
        Assert.Equal(25, tail);
        Assert.NotEqual(backpack, wings);
        Assert.NotEqual(wings, tail);
        Assert.NotEqual(backpack, tail);
        Assert.Equal(19, handheld);
        Assert.Equal(13, wig);
        Assert.Equal(12, glasses);
        Assert.Equal(18, leftEarring);
        Assert.Equal(17, rightEarring);
        Assert.NotEqual(glasses, necklace);
        Assert.NotEqual(18, handheld);
        Assert.NotEqual(24, leftEarring);
        Assert.NotEqual(14, leftEarring);
        Assert.NotEqual(13, glasses);
        Assert.NotEqual(15, glasses);
        Assert.Equal(1u << 13, ItemEntityMapper.ResolveBodyspot(10900000, 51));
        Assert.Equal(1u << 12, ItemEntityMapper.ResolveBodyspot(10800000, 11));
        Assert.Equal(1u << 14, ItemEntityMapper.ResolveBodyspot(11600060, 12));
        Assert.Equal(1u << 14, ItemEntityMapper.ResolveBodyspot(11600010, 0));
        Assert.Equal(1u << 16, ItemEntityMapper.ResolveBodyspot(11700020, 14));
        Assert.Equal(1u << 12, ItemEntityMapper.ResolveBodyspot(11800030, 11));
        Assert.Equal(1u << 19, ItemEntityMapper.ResolveBodyspot(11200000, 0));
        Assert.Equal(1u << 18, ItemEntityMapper.ResolveBodyspot(10800080, 15));
        Assert.Equal(1u << 17, ItemEntityMapper.ResolveBodyspot(10800150, 16));
    }
}
