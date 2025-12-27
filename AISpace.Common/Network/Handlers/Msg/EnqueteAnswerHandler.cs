
using AISpace.Common.Network.Packets.Msg;

namespace AISpace.Common.Network.Handlers.Msg;

public class EnqueteAnswerHandler : IPacketHandler
{
    public PacketType RequestType => PacketType.Msg_EnqueteAnswerRequest;

    public PacketType ResponseType => PacketType.Msg_EnqueteAnswerResponse;

    public MessageDomain Domains => MessageDomain.Msg;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, ClientConnection connection, CancellationToken ct = default)
    {
        await connection.SendAsync(ResponseType, new EnqueteAnswerResponse(0), ct);
    }
}
