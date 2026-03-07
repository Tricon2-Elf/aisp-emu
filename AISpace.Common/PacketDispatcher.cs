using AISpace.Common.Game;
using AISpace.Network;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AISpace.Common;

public class PacketDispatcher
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PacketDispatcher> _logger;

    public PacketDispatcher(IServiceScopeFactory scopeFactory, ILogger<PacketDispatcher> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task DispatchAsync(MessageDomain domain, PacketType type, byte[] payload, IPlayerSession session, CancellationToken ct = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handlers = scope.ServiceProvider.GetServices<IPacketHandler>();
        var handler = handlers.FirstOrDefault(h => h.Domain == domain && h.RequestType == type);
        if (handler != null)
        {
            await handler.HandleAsync(payload, session, ct);
        }
        else
        {
            _logger.LogWarning("No handler for {Domain}:{PacketType} (payload length: {Length}). Raw data: {Hex}", domain, type, payload.Length, BitConverter.ToString(payload));
        }
    }
}
