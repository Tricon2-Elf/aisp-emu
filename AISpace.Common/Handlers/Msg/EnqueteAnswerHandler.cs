using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Packets.Msg;

namespace AISpace.Common.Handlers.Msg;

public class EnqueteAnswerHandler : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.EnqueteAnswerRequest;

    public PacketType ResponseType => PacketType.EnqueteAnswerResponse;

    public ServerType ServerType => ServerType.Msg;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, IPlayerSession session, CancellationToken ct = default)
    {
        await session.SendAsync(ResponseType, new EnqueteAnswerResponse(0).ToBytes(), ct);
    }
}
