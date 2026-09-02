using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Data;
using aisp.Network.Packets.Area;

namespace aisp.Common.Handlers.Area;

public class AreaFriendGetListDataHandler(IFriendRepository friends, SharedState state)
    : IPacketHandler,
        IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.FriendGetListDataRequest;

    public PacketType ResponseType => PacketType.FriendGetListDataResponse;

    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        if (session.CharacterId > int.MaxValue)
        {
            await session.SendAsync(ResponseType, new FriendGetListDataResponse().ToBytes(), ct);
            return;
        }

        var characters = await friends.GetFriendsAsync((int)session.CharacterId, ct);
        FriendData[] friendData =
        [
            .. characters.Select(character => new FriendData((uint)character.Id, character.Name)),
        ];
        bool[] online =
        [
            .. characters.Select(character =>
                state.GetAreaSessionByCharacterId((uint)character.Id) is not null
            ),
        ];
        var response = new FriendGetListDataResponse(friendData, online);
        await session.SendAsync(ResponseType, response.ToBytes(), ct);
    }
}
