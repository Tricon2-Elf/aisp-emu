using AISpace.Network.Data;
using AISpace.Network.Packets.Area;

namespace AISpace.Common.Tests;

public class EquipOrderListResponseTests
{
    [Fact]
    public void ToBytes_writes_chara_orders_before_job_count()
    {
        var payload = new EquipOrderListResponse().ToBytes();

        Assert.Equal(0u, BitConverter.ToUInt32(payload, 0));
        Assert.Equal(8u, BitConverter.ToUInt32(payload, 4));
        Assert.Equal(101u, BitConverter.ToUInt32(payload, 8));
        Assert.Equal(285 * 8 + 8 + 4, payload.Length);

        var jobCountOffset = 8 + 285 * 8;
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
    public void ForGender_allows_avatar_and_robo_controller_types()
    {
        var pants = CharaOrderData.ForGender(2).First(o => o.Category == 102);
        Assert.Equal(CharaOrderData.ControllerAvatarOrRobo, pants.LimitByte1);
        Assert.Equal(2, pants.LimitByte2);

        var shirt = CharaOrderData.ForGender(1).First(o => o.Category == 101);
        Assert.Equal(CharaOrderData.ControllerAvatarOrRobo, shirt.LimitByte1);
        Assert.Equal(1, shirt.LimitByte2);
    }
}
