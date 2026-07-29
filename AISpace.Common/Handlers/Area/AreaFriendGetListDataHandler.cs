using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Packets.Area;

namespace AISpace.Common.Handlers.Area;

public class AreaFriendGetListDataHandler : IPacketHandler, IRequiresAuthenticatedSession
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
        var response = new FriendGetListDataResponse();
        await session.SendAsync(ResponseType, response.ToBytes(), ct);
    }
}
