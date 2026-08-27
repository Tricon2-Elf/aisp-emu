using aisp.Network.Data;
using aisp.Network.Packets.Area;

namespace aisp.Common.Tests;

public class EquipOrderListResponseTests
{
    [Fact]
    public void ToBytes_writes_chara_orders_before_job_count()
    {
        var payload = new EquipOrderListResponse().ToBytes();
        var orderCount = CharaOrderData.WardrobeOrders.Count;

        Assert.Equal(0u, BitConverter.ToUInt32(payload, 0));
        Assert.Equal((uint)orderCount, BitConverter.ToUInt32(payload, 4));
        Assert.Equal(100u, BitConverter.ToUInt32(payload, 8));
        Assert.Equal(285 * orderCount + 8 + 4, payload.Length);

        var jobCountOffset = 8 + 285 * orderCount;
        Assert.Equal(0u, BitConverter.ToUInt32(payload, jobCountOffset));
        Assert.Equal(jobCountOffset + 4, payload.Length);
    }

    [Fact]
    public void CharaOrderData_matches_client_default_limit_bytes()
    {
        var shirt = new CharaOrderData(101, 1, 1).ToBytes();
        Assert.Equal(285, shirt.Length);
        Assert.Equal(101u, BitConverter.ToUInt32(shirt, 0));
        Assert.Equal(1, shirt[0xC5]);
        Assert.Equal(1, shirt[0xC6]);
    }

    [Fact]
    public void WardrobeOrders_allow_avatar_and_robo_without_gender_lock()
    {
        var pants = CharaOrderData.WardrobeOrders.First(o => o.Category == 102);
        Assert.Equal(CharaOrderData.ControllerAvatarOrRobo, pants.LimitByte1);
        Assert.Equal(CharaOrderData.GenderUnrestricted, pants.LimitByte2);

        var shirt = CharaOrderData.ForGender(1).First(o => o.Category == 101);
        Assert.Equal(CharaOrderData.ControllerAvatarOrRobo, shirt.LimitByte1);
        Assert.Equal(CharaOrderData.GenderUnrestricted, shirt.LimitByte2);

        var femaleShirt = CharaOrderData.ForGender(2).First(o => o.Category == 101);
        Assert.Equal(shirt.LimitByte2, femaleShirt.LimitByte2);

        var hat = CharaOrderData.WardrobeOrders.First(o => o.Category == 100);
        Assert.Equal(CharaOrderData.ControllerAvatarOrRobo, hat.LimitByte1);
        Assert.Equal(CharaOrderData.GenderUnrestricted, hat.LimitByte2);

        var accessory = CharaOrderData.WardrobeOrders.First(o => o.Category == 108);
        Assert.Equal(CharaOrderData.ControllerAvatarOrRobo, accessory.LimitByte1);
        Assert.Equal(CharaOrderData.GenderUnrestricted, accessory.LimitByte2);

        var wig = CharaOrderData.WardrobeOrders.First(o => o.Category == 109);
        Assert.Equal(CharaOrderData.ControllerAvatarOrRobo, wig.LimitByte1);
        Assert.Equal(CharaOrderData.GenderUnrestricted, wig.LimitByte2);
    }
}
