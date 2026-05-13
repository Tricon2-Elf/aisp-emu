using AISpace.Common.Game;
using AISpace.Network;
using AISpace.Network.Packets.Msg;

namespace AISpace.Common.Handlers.Msg;

public class CircleTalkHandler : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.CircleTalkRequest;
    public PacketType ResponseType => PacketType.CircleChatPostResponse;
    public ServerType ServerType => ServerType.Area;

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, IPlayerSession session, CancellationToken ct = default)
    {
        var req = CircleTalkRequest.FromBytes(payload.Span);
        await session.SendAsync(ResponseType, new CmdExecResponse(req.MessageId, 0).ToBytes(), ct);
    }
}
