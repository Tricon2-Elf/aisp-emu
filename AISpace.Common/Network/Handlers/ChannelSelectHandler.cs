using AISpace.Common.Game;
using AISpace.Common.Network.Packets.Msg;

namespace AISpace.Common.Network.Handlers;

public class ChannelSelectHandler : IPacketHandler
{
    public PacketType RequestType => PacketType.ChannelSelectRequest;

    public PacketType ResponseType => PacketType.ChannelSelectResponse;

    public MessageDomain Domains => MessageDomain.Msg;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, ClientConnection connection, CancellationToken ct = default)
    {
        _ = ChannelSelectRequest.FromBytes(payload.Span);
        var response = new ChannelSelectResponse(0, new ServerInfo("127.0.0.1", 50054), 10990200, 10990200);
        await connection.SendAsync(ResponseType, response.ToBytes(), ct);
    }
}
