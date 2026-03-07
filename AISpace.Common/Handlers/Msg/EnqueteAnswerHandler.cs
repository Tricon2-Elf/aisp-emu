using AISpace.Common.Network.Packets.Msg;
using AISpace.Network;

namespace AISpace.Common.Handlers.Msg;

public class EnqueteAnswerHandler : IPacketHandler
{
    public PacketType RequestType => PacketType.EnqueteAnswerRequest;

    public PacketType ResponseType => PacketType.EnqueteAnswerResponse;

    public MessageDomain Domain => MessageDomain.Msg;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, ClientConnection connection, CancellationToken ct = default)
    {
        await connection.SendAsync(ResponseType, new EnqueteAnswerResponse(0), ct);
    }
}
