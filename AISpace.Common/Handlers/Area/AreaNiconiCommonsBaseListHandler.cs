using AISpace.Network.Packets.Area;
using AISpace.Network;

using AISpace.Common.Game;

namespace AISpace.Common.Handlers.Area;

public class AreaNiconiCommonsBaseListHandler : IPacketHandler
{
    public PacketType RequestType => PacketType.NiconiCommonsBaseListRequest;

    public PacketType ResponseType => PacketType.NiconiCommonsBaseListResponse;

    public MessageDomain Domain => MessageDomain.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, IPlayerSession session, CancellationToken ct = default)
    {
        var response = new NiconiCommonsBaseListResponse();
        await session.SendAsync(ResponseType, response.ToBytes(), ct);
    }
}
