using AISpace.Common.Game;
using AISpace.Network;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AISpace.Common;

public class PacketDispatcher(IServiceScopeFactory scopeFactory, ILogger<PacketDispatcher> logger, ILoggerFactory loggerFactory)
{
    private readonly ILogger _missingPacketsLogger = loggerFactory.CreateLogger("AISpace.MissingPackets");

    public async Task DispatchAsync(MessageDomain domain, PacketType type, byte[] payload, IPlayerSession session, CancellationToken ct = default)
    {
        using var scope = scopeFactory.CreateScope();
        var handlers = scope.ServiceProvider.GetServices<IPacketHandler>();
        var handler = handlers.FirstOrDefault(h => h.Domain == domain && h.RequestType == type);
        if (handler != null)
        {
            await handler.HandleAsync(payload, session, ct);
        }
        else
        {
            var message = "No handler for {Domain}:{PacketType} (payload length: {Length}). Raw data: {Hex}";
            logger.LogWarning(message, domain, type, payload.Length, BitConverter.ToString(payload));
            _missingPacketsLogger.LogWarning(message, domain, type, payload.Length, BitConverter.ToString(payload));
        }
    }
}
