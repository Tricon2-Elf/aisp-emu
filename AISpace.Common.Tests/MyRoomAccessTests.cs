using AISpace.Common.DAL.Entities;
using AISpace.Common.Game;
using AISpace.Network;

namespace AISpace.Common.Tests;

public sealed class MyRoomAccessTests
{
    [Fact]
    public void Owner_CanAlwaysEnter()
    {
        var room = new Room { OwnerCharacterId = 10, Security = MyRoomSecurity.Private };

        Assert.True(MyRoomAccess.CanEnter(room, 10, visitorCircleId: 1, ownerCircleId: 2));
    }

    [Theory]
    [InlineData(MyRoomSecurity.Private, false)]
    [InlineData(MyRoomSecurity.Public, true)]
    [InlineData(MyRoomSecurity.FriendsOnly, false)]
    public void Guest_RespectsSecurityWithoutCircle(MyRoomSecurity security, bool expected)
    {
        var room = new Room { OwnerCharacterId = 10, Security = security };

        Assert.Equal(expected, MyRoomAccess.CanEnter(room, 20, null, null));
    }

    [Fact]
    public void CircleMembersOnly_RequiresSharedCircle()
    {
        var room = new Room { OwnerCharacterId = 10, Security = MyRoomSecurity.CircleMembersOnly };

        Assert.False(MyRoomAccess.CanEnter(room, 20, visitorCircleId: 1, ownerCircleId: 2));
        Assert.True(MyRoomAccess.CanEnter(room, 20, visitorCircleId: 7, ownerCircleId: 7));
    }
}
