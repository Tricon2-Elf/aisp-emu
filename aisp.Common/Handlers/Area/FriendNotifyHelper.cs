using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Packets.Area;

namespace aisp.Common.Handlers.Area;

public static class FriendNotifyHelper
{
    public static Task NotifyLoginAsync(
        IFriendRepository friends,
        SharedState state,
        int characterId,
        CancellationToken ct
    ) =>
        NotifyPresenceAsync(
            friends,
            state,
            characterId,
            PacketType.NotifyFriendListAvatarLogin,
            ct
        );

    public static Task NotifyLogoutAsync(
        IFriendRepository friends,
        SharedState state,
        int characterId,
        CancellationToken ct
    ) =>
        NotifyPresenceAsync(
            friends,
            state,
            characterId,
            PacketType.NotifyFriendListAvatarLogout,
            ct
        );

    private static async Task NotifyPresenceAsync(
        IFriendRepository friends,
        SharedState state,
        int characterId,
        PacketType packetType,
        CancellationToken ct
    )
    {
        var payload = new FriendAvatarPresenceNotify((uint)characterId).ToBytes();
        foreach (var friend in await friends.GetFriendsAsync(characterId, ct))
        {
            var client = state.GetAreaSessionByCharacterId((uint)friend.Id);
            if (client is not null)
                await client.SendAsync(packetType, payload, ct);
        }
    }
}
