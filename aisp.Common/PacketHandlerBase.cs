using aisp.Common.Game;
using aisp.Network;

namespace aisp.Common;

public enum ServerType
{
    Auth = 1,
    Area = 2,
    Msg = 3,
}

public interface IPacketHandler
{
    PacketType RequestType { get; }
    PacketType ResponseType { get; }
    ServerType ServerType { get; }
    Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    );
}

public interface IRequiresAuthenticatedSession;

public abstract class PacketHandlerBase<TRequest, TResponse> : IPacketHandler
    where TRequest : IIncomingPacket<TRequest>
    where TResponse : IOutgoingPacket
{
    public abstract PacketType RequestType { get; }
    public abstract PacketType ResponseType { get; }
    public abstract ServerType ServerType { get; }

    public abstract Task<TResponse?> HandleAsync(
        TRequest request,
        IPlayerSession session,
        CancellationToken ct = default
    );

    public async Task HandleAsync(
        ReadOnlyMemory<byte> payload,
        IPlayerSession session,
        CancellationToken ct = default
    )
    {
        var request = TRequest.FromBytes(payload.Span);
        var response = await HandleAsync(request, session, ct);
        if (response != null)
            await session.SendAsync(ResponseType, response.ToBytes(), ct);
    }
}
