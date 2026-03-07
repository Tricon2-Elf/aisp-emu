using AISpace.Network.Packets.Area;
using AISpace.Network;

using AISpace.Common.Game;

namespace AISpace.Common.Handlers.Area;

public class AreaFriendGetListDataHandler : IPacketHandler
{
    public PacketType RequestType => PacketType.FriendGetListDataRequest;

    public PacketType ResponseType => PacketType.FriendGetListDataResponse;

    public MessageDomain Domain => MessageDomain.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, IPlayerSession session, CancellationToken ct = default)
    {
        var response = new FriendGetListDataResponse();
        await session.SendAsync(ResponseType, response.ToBytes(), ct);
    }
}
