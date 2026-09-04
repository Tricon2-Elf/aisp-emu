using aisp.Common.Game;
using aisp.Network;
using aisp.Network.Packets.Msg;

namespace aisp.Common.Handlers.Msg;

/// <summary>Completes the placard interaction flow when no comments have been posted yet.</summary>
public sealed class GetPlacardCommentLogHandler : IPacketHandler, IRequiresAuthenticatedSession
{
    public PacketType RequestType => PacketType.GetPlacardCommentLogRequest;
    public PacketType ResponseType => PacketType.GetPlacardCommentLogResponse;
    public ServerType ServerType => ServerType.Msg;

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var request = GetPlacardCommentLogRequest.FromBytes(payload.Span);
        await session.SendAsync(ResponseType, new GetPlacardCommentLogResponse(0).ToBytes(), ct);
        await session.SendAsync(
            PacketType.NotifyPlacardCommentLog,
            new NotifyPlacardCommentLog(
                0,
                request.PlacardId,
                [new PlacardCommentLogEntry("AISpace", "Welcome to this Friend Link placard!")]
            ).ToBytes(),
            ct
        );
    }
}
