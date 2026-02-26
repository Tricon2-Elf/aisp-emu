using AISpace.Common.Network.Handlers;
using Microsoft.Extensions.Logging;

namespace AISpace.Common.Network;

public class PacketDispatcher
{
    private readonly Dictionary<(MessageDomain, PacketType), IPacketHandler> _handlers;
    private readonly ILogger<PacketDispatcher> _logger;

    public PacketDispatcher(IEnumerable<IPacketHandler> allHandlers, ILogger<PacketDispatcher> logger)
    {
        _logger = logger;
        _handlers = [];
        foreach (var handler in allHandlers)
            _handlers[(handler.Domain, handler.RequestType)] = handler;
    }

    public async Task DispatchAsync(MessageDomain domain, PacketType type, byte[] payload, ClientConnection connection, CancellationToken ct = default)
    {
        if (_handlers.TryGetValue((domain, type), out var handler))
        {
            await handler.HandleAsync(payload, connection, ct);
        }
        else
        {
            _logger.LogWarning("No handler for {Domain}:{PacketType} (payload length: {Length}). Raw data: {Hex}", domain, type, payload.Length, BitConverter.ToString(payload));
        }
    }
}
