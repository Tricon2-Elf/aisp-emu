using aisp.Common.Config;
using aisp.Common.DAL.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace aisp.Server.Services;

public sealed class ChatLogPruneService(
    IServiceScopeFactory scopeFactory,
    IOptions<ChatLogOptions> options,
    ILogger<ChatLogPruneService> logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var days = options.Value.RetentionDays;
        if (days <= 0)
        {
            logger.LogInformation("Chat log prune is disabled");
            return;
        }

        logger.LogInformation("Chat log prune enabled: keep {Days} days", days);
        await PruneAsync(days, ct);
        using var timer = new PeriodicTimer(TimeSpan.FromHours(24));
        while (await timer.WaitForNextTickAsync(ct))
            await PruneAsync(days, ct);
    }

    private async Task PruneAsync(int days, CancellationToken ct)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var chatLog = scope.ServiceProvider.GetRequiredService<IChatLogRepository>();
            var cutoff = DateTime.UtcNow.AddDays(-days);
            var removed = await chatLog.PruneOlderThanAsync(cutoff, ct);
            if (removed > 0)
                logger.LogInformation(
                    "Pruned {Count} chat messages older than {Cutoff:u}",
                    removed,
                    cutoff
                );
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to prune chat messages");
        }
    }
}
