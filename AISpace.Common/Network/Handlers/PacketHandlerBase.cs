using AISpace.Common.Network;
using AISpace.Common.Network.Packets;

namespace AISpace.Common.Network.Handlers;

/// <summary>
/// Base class for packet handlers that work with strongly-typed request and response.
/// Deserializes the payload to TRequest, calls the typed HandleAsync, and sends the response if non-null.
/// </summary>
public abstract class PacketHandlerBase<TRequest, TResponse> : IPacketHandler
    where TRequest : IPacket<TRequest>
    where TResponse : IPacket<TResponse>
{
    public abstract PacketType RequestType { get; }
    public abstract PacketType ResponseType { get; }
    public abstract MessageDomain Domain { get; }

    /// <summary>
    /// Handle the deserialized request. Return a response to send, or null if the handler already sent (e.g. error response) or no response is needed.
    /// </summary>
    public abstract Task<TResponse?> HandleAsync(TRequest request, ClientConnection connection, CancellationToken ct = default);

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, ClientConnection connection, CancellationToken ct = default)
    {
        var request = TRequest.FromBytes(payload.Span);
        var response = await HandleAsync(request, connection, ct);
        if (response != null)
            await connection.SendAsync(ResponseType, response.ToBytes(), ct);
    }
}
