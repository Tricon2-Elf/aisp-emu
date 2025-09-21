using AISpace.Common.Network.Packets.Msg;

namespace AISpace.Common.Network.Handlers;

public class ItemGetBaseListHandler : IPacketHandler
{
    public PacketType RequestType => PacketType.ItemGetBaseListRequest;

    public PacketType ResponseType => PacketType.ItemGetBaseListResponse;

    public MessageDomain Domains => MessageDomain.Msg;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, ClientConnection connection, CancellationToken ct = default)
    {
        var response = new ItemGetBaseListResponse();
        await connection.SendAsync(ResponseType, response.ToBytes(), ct);
    }
}
