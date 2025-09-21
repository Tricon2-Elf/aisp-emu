using AISpace.Common.Network.Packets.Msg;
using NLog;

namespace AISpace.Common.Network.Handlers;

public class PostTalkHandler : IPacketHandler
{
    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

    public PacketType RequestType => PacketType.PostTalkRequest;

    public PacketType ResponseType => PacketType.PostTalkRequest;

    public MessageDomain Domains => MessageDomain.Msg;

    public Task HandleAsync(ReadOnlyMemory<byte> payload, ClientConnection connection, CancellationToken ct = default)
    {
        var chatMessage = PostTalkRequest.FromBytes(payload.Span);
        _logger.Info($"User says {chatMessage.Message} | MID{chatMessage.MessageID} | DID{chatMessage.DistID} | BID{chatMessage.BalloonID}");
        return Task.CompletedTask;
    }
}
