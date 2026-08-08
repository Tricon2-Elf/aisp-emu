using AISpace.Common.DAL.Entities;
using AISpace.Network;

namespace AISpace.Common.Game;

/// <summary>
/// Server-side My Room visit ACL. Friend lists are not persisted yet, so
/// FriendsOnly denies non-owners and FriendsAndCircleMembers falls back to
/// circle membership (or ownership) rather than a friend link.
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

    public static bool CanEnter(Room room, int visitorCharacterId, bool sharesCircle)
    {
        if (room.OwnerCharacterId == visitorCharacterId)
            return true;

        return room.Security switch
        {
            MyRoomSecurity.Public => true,
            MyRoomSecurity.Private => false,
            MyRoomSecurity.CircleMembersOnly => sharesCircle,
            MyRoomSecurity.FriendsOnly => false,
            MyRoomSecurity.FriendsAndCircleMembers => sharesCircle,
            _ => false,
        };
    }
}
