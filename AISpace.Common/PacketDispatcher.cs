using AISpace.Common.Game;
using AISpace.Network;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AISpace.Common;

public sealed class PacketDispatcher
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PacketDispatcher> _logger;
    private readonly ILogger _missingPacketsLogger;
    private readonly Dictionary<(ServerType ServerType, PacketType PacketType), Type> _handlerTypes;

    public PacketDispatcher(IServiceScopeFactory scopeFactory, ILogger<PacketDispatcher> logger, ILoggerFactory loggerFactory)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _missingPacketsLogger = loggerFactory.CreateLogger("AISpace.MissingPackets");
        using var bootstrapScope = scopeFactory.CreateScope();
        _handlerTypes = bootstrapScope.ServiceProvider.GetServices<IPacketHandler>().ToDictionary(h => (h.ServerType, h.RequestType), h => h.GetType());
    }

    public async Task DispatchAsync(ServerType ServerType, PacketType type, byte[] payload, IPlayerSession session, CancellationToken ct = default)
    {
        if (!_handlerTypes.TryGetValue((ServerType, type), out var handlerType))
        {
            var message = "No handler for {ServerType}:{PacketType} (payload length: {Length}). Raw data: {Hex}";
            _logger.LogWarning(message, ServerType, type, payload.Length, BitConverter.ToString(payload));
            _missingPacketsLogger.LogWarning(message, ServerType, type, payload.Length, BitConverter.ToString(payload));
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var handler = (IPacketHandler)ActivatorUtilities.CreateInstance(scope.ServiceProvider, handlerType);
        if (handler is IRequiresAuthenticatedSession && !session.IsAuthenticated)
        {
            _logger.LogWarning("Rejecting unauthenticated packet {ServerType}:{PacketType} from client {ClientId}", ServerType, type, session.ConnectionId);
            return;
        }

        await handler.HandleAsync(payload, session, ct);
    }
}
