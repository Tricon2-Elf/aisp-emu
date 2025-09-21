using AISpace.Common.Network.Packets.Msg;

namespace AISpace.Common.Network.Handlers;

public class AvatarSelectHandler : IPacketHandler
{
    public PacketType RequestType => PacketType.AvatarSelectRequest;

    public PacketType ResponseType => PacketType.AvatarSelectResponse;

    public MessageDomain Domains => MessageDomain.Msg;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, ClientConnection connection, CancellationToken ct = default)
    {
        _ = AvatarSelectRequest.FromBytes(payload.Span);
        var response = new AvatarSelectResponse(0);
        await connection.SendAsync(ResponseType, response.ToBytes(), ct);
    }
}
