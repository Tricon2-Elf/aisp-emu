using aisp.Common.Game;

namespace aisp.Common.Tests;

public sealed class UserRoleNamePlateTests
{
    [Theory]
    [InlineData(UserRole.User, 0u)]
    [InlineData(UserRole.Moderator, 2u)]
    [InlineData(UserRole.Admin, 0xFFFFFFFFu)]
    [InlineData(UserRole.ServerAdmin, 0xFFFFFFFFu)]
    public void ToNamePlate_MapsRole(UserRole role, uint expected) =>
        Assert.Equal(expected, role.ToNamePlate());
}
