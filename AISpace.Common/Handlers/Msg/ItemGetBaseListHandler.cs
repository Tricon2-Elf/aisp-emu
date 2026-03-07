using AISpace.Network.Packets.Msg;
using AISpace.Network;

using AISpace.Common.Game;

namespace AISpace.Common.Handlers.Msg;

public class ItemGetBaseListHandler : IPacketHandler
{
    public PacketType RequestType => PacketType.ItemGetBaseListRequest;
    public PacketType ResponseType => PacketType.ItemGetBaseListResponse;
    public MessageDomain Domain => MessageDomain.Msg;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, IPlayerSession session, CancellationToken ct = default)
    {
        var response = new ItemGetBaseListResponse();
        await session.SendAsync(ResponseType, response.ToBytes(), ct);
    }
}
