using aisp.Common.Config;
using aisp.Common.DAL.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace aisp.Server.Services;

/// <summary>
/// Weekly settlement of drama disc sales. Every purchase made before the most recent cutoff (Saturday 05:00
/// Japan time by default) has its author share moved into the author's collectable balance, which the shop's
/// 売上担当 clerk pays out. Settling is idempotent, so the service simply re-checks on an interval and also
/// catches up after downtime.
/// </summary>
public sealed class AdventureSettlementService(
    IServiceScopeFactory scopeFactory,
    IOptions<ServerOptions> options,
    ILogger<AdventureSettlementService> logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var settlement = options.Value.AdventureSettlement;
        var interval = TimeSpan.FromMinutes(Math.Max(1, settlement.CheckIntervalMinutes));
        logger.LogInformation(
            "Drama disc sales settle weekly on {Day} {Time} {Zone}; next cutoff {Next:u}",
            settlement.DayOfWeek,
            settlement.Time,
            settlement.TimeZone,
            settlement.GetNextCutoffUtc(DateTime.UtcNow)
        );
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var cutoff = settlement.GetLastCutoffUtc(DateTime.UtcNow);
                await using var scope = scopeFactory.CreateAsyncScope();
                var shop = scope.ServiceProvider.GetRequiredService<IAdventureShopRepository>();
                var settled = await shop.SettleAsync(cutoff, ct);
                if (settled > 0)
                    logger.LogInformation(
                        "Settled {Count} drama disc purchase(s) made before {Cutoff:u}",
                        settled,
                        cutoff
                    );
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Drama disc settlement failed; will retry");
            }

            try
            {
                await Task.Delay(interval, ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
