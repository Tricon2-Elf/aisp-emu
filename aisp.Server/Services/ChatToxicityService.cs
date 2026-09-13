using aisp.Common.Config;
using aisp.Common.DAL.Repositories;
using aisp.Common.Services.Toxicity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace aisp.Server.Services;

/// <summary>
/// Background worker: downloads models, classifies live chat first, then slowly
/// backfills unclassified rows. Never runs on the Msg packet loop.
/// </summary>
public sealed class ChatToxicityService : BackgroundService, IChatToxicityClassifier
{
    public const string HttpClientName = "ChatToxicity";
    const int LiveChannelCapacity = 256;

    readonly IServiceScopeFactory _scopeFactory;
    readonly IHttpClientFactory _httpClientFactory;
    readonly IOptions<ChatToxicityOptions> _options;
    readonly IOptions<ServerOptions> _serverOptions;
    readonly ILogger<ChatToxicityService> _logger;
    readonly ILoggerFactory _loggerFactory;
    readonly Channel<(long Id, string Text)> _live = Channel.CreateBounded<(long, string)>(
        new BoundedChannelOptions(LiveChannelCapacity)
        {
            FullMode = BoundedChannelFullMode.DropWrite,
            SingleReader = true,
            SingleWriter = false,
        }
    );

    ToxicityClassifier? _classifier;
    int _dropped;

    public ChatToxicityService(
        IServiceScopeFactory scopeFactory,
        IHttpClientFactory httpClientFactory,
        IOptions<ChatToxicityOptions> options,
        IOptions<ServerOptions> serverOptions,
        ILogger<ChatToxicityService> logger,
        ILoggerFactory loggerFactory
    )
    {
        _scopeFactory = scopeFactory;
        _httpClientFactory = httpClientFactory;
        _options = options;
        _serverOptions = serverOptions;
        _logger = logger;
        _loggerFactory = loggerFactory;
    }

    public bool TryEnqueue(long id, string message)
    {
        if (_classifier is null || string.IsNullOrWhiteSpace(message))
            return false;

        if (_live.Writer.TryWrite((id, message)))
            return true;

        var dropped = Interlocked.Increment(ref _dropped);
        if (dropped == 1 || dropped % 100 == 0)
            _logger.LogWarning(
                "Chat toxicity live queue full; dropped {Count} message(s)",
                dropped
            );
        return false;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var opts = _options.Value;
        if (!opts.Enabled)
        {
            _logger.LogInformation("Chat toxicity classification is disabled");
            return;
        }

        var modelRoot = ChatToxicityPaths.ResolveModelRoot(opts, _serverOptions.Value.DbOptions);
        Directory.CreateDirectory(modelRoot);

        try
        {
            var http = _httpClientFactory.CreateClient(HttpClientName);
            var downloader = new ToxicityModelDownloader(
                http,
                _loggerFactory.CreateLogger<ToxicityModelDownloader>()
            );
            await downloader.EnsureModelsAsync(modelRoot, stoppingToken);

            var classifierOptions = ToxicityClassifierOptions.FromModelRoot(modelRoot) with
            {
                RobertaThreshold = opts.RobertaThreshold,
                InsultThreshold = opts.InsultThreshold,
                DistilBertThreshold = opts.DistilBertThreshold,
            };
            _classifier = new ToxicityClassifier(classifierOptions, _logger);
            _logger.LogInformation(
                "Chat toxicity classification ready (model root {Root})",
                modelRoot
            );
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            return;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Chat toxicity failed to start; classification disabled for this process"
            );
            _classifier = null;
            return;
        }

        using var idleCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        var idleTask =
            opts.IdleUnloadMinutes > 0
                ? RunIdleUnloadAsync(TimeSpan.FromMinutes(opts.IdleUnloadMinutes), idleCts.Token)
                : Task.CompletedTask;

        try
        {
            await RunClassifyLoopAsync(opts, stoppingToken);
        }
        finally
        {
            idleCts.Cancel();
            try
            {
                await idleTask;
            }
            catch (OperationCanceledException)
            {
                // expected
            }

            _classifier?.Dispose();
            _classifier = null;
        }
    }

    async Task RunClassifyLoopAsync(ChatToxicityOptions opts, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var drained = await DrainLiveAsync(ct);
                if (drained)
                    continue;

                if (opts.BackfillDelayMs > 0 && opts.BackfillBatchSize > 0)
                {
                    var didBackfill = await BackfillBatchAsync(opts, ct);
                    if (didBackfill)
                    {
                        await Task.Delay(opts.BackfillDelayMs, ct);
                        continue;
                    }
                }

                using var waitCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                waitCts.CancelAfter(TimeSpan.FromSeconds(5));
                try
                {
                    await _live.Reader.WaitToReadAsync(waitCts.Token);
                }
                catch (OperationCanceledException) when (!ct.IsCancellationRequested)
                {
                    // timed out — loop and try backfill again
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Chat toxicity worker error");
                await Task.Delay(1000, ct);
            }
        }
    }

    async Task<bool> DrainLiveAsync(CancellationToken ct)
    {
        var any = false;
        while (_live.Reader.TryRead(out var item))
        {
            any = true;
            await ClassifyAndStoreAsync(item.Id, item.Text, ct);
        }

        return any;
    }

    async Task<bool> BackfillBatchAsync(ChatToxicityOptions opts, CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var chatLog = scope.ServiceProvider.GetRequiredService<IChatLogRepository>();
        var take = Math.Clamp(opts.BackfillBatchSize, 1, 64);
        var rows = await chatLog.ListUnclassifiedAsync(take, ct);
        if (rows.Count == 0)
            return false;

        var classifier = _classifier;
        if (classifier is null)
            return false;

        try
        {
            var texts = rows.Select(r => r.Message).ToList();
            var results = classifier.ClassifyMany(texts);
            for (var i = 0; i < rows.Count; i++)
            {
                var reason = ChatToxicityReason.Format(results[i]);
                await chatLog.SetToxicityAsync(rows[i].Id, results[i].IsToxic, reason, ct);
            }

            _logger.LogDebug("Chat toxicity backfill classified {Count} row(s)", rows.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chat toxicity backfill batch failed");
        }

        return true;
    }

    async Task ClassifyAndStoreAsync(long id, string text, CancellationToken ct)
    {
        var classifier = _classifier;
        if (classifier is null)
            return;

        try
        {
            var result = classifier.Classify(text);
            var reason = ChatToxicityReason.Format(result);
            await using var scope = _scopeFactory.CreateAsyncScope();
            var chatLog = scope.ServiceProvider.GetRequiredService<IChatLogRepository>();
            await chatLog.SetToxicityAsync(id, result.IsToxic, reason, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to classify chat message {Id}", id);
        }
    }

    async Task RunIdleUnloadAsync(TimeSpan idle, CancellationToken ct)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(60));
        while (await timer.WaitForNextTickAsync(ct))
        {
            try
            {
                _classifier?.UnloadIdle(idle);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Chat toxicity idle unload failed");
            }
        }
    }
}
