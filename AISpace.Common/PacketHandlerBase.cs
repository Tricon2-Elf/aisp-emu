using AISpace.Common.Game;
using AISpace.Network;

namespace AISpace.Common;

public enum MessageDomain
{
    Auth = 1,
    Area = 2,
    Msg = 3,
}

public interface IPacketHandler
{
    PacketType RequestType { get; }
    PacketType ResponseType { get; }
    MessageDomain Domain { get; }
    Task HandleAsync(ReadOnlyMemory<byte> payload, IPlayerSession session, CancellationToken ct = default);
}

public abstract class PacketHandlerBase<TRequest, TResponse> : IPacketHandler
    where TRequest : IPacket<TRequest>
    where TResponse : IPacket<TResponse>
{
    public abstract PacketType RequestType { get; }
    public abstract PacketType ResponseType { get; }
    public abstract MessageDomain Domain { get; }

    public abstract Task<TResponse?> HandleAsync(TRequest request, IPlayerSession session, CancellationToken ct = default);

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, IPlayerSession session, CancellationToken ct = default)
    {
        var request = TRequest.FromBytes(payload.Span);
        var response = await HandleAsync(request, session, ct);
        if (response != null)
            await session.SendAsync(ResponseType, response.ToBytes(), ct);
    }
}
