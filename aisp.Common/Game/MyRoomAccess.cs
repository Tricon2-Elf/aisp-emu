using aisp.Common.DAL.Entities;
using aisp.Network;

namespace aisp.Common.Game;

/// <summary>
/// Server-side My Room visit ACL.
/// </summary>
public static class MyRoomAccess
{
    public static bool CanEnter(
        Room room,
        int visitorCharacterId,
        int? visitorCircleId,
        int? ownerCircleId
    ) =>
        CanEnter(
            room,
            visitorCharacterId,
            sharesCircle: visitorCircleId is > 0 && visitorCircleId == ownerCircleId
        );

    public static bool CanEnter(
        Room room,
        int visitorCharacterId,
        bool sharesCircle,
        bool isFriend = false
    )
    {
        if (room.OwnerCharacterId == visitorCharacterId)
            return true;

        return room.Security switch
        {
            MyRoomSecurity.Public => true,
            MyRoomSecurity.Private => false,
            MyRoomSecurity.CircleMembersOnly => sharesCircle,
            MyRoomSecurity.FriendsOnly => isFriend,
            MyRoomSecurity.FriendsAndCircleMembers => isFriend || sharesCircle,
            _ => false,
        };
    }
}
