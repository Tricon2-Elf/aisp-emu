using AISpace.Network.Packets.Msg;
using AISpace.Network;

using AISpace.Common.Game;

namespace AISpace.Common.Handlers.Msg;

public class EnqueteAnswerHandler : IPacketHandler
{
    public PacketType RequestType => PacketType.EnqueteAnswerRequest;

    public PacketType ResponseType => PacketType.EnqueteAnswerResponse;

    public MessageDomain Domain => MessageDomain.Msg;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, IPlayerSession session, CancellationToken ct = default)
    {
        await session.SendAsync(ResponseType, new EnqueteAnswerResponse(0).ToBytes(), ct);
    }
}
