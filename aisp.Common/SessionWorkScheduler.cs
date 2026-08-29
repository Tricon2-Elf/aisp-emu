using System.Collections.Concurrent;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;

namespace aisp.Common;

/// <summary>
/// Routes work onto a per-session serial queue so one session's handler cannot block other sessions.
/// Packets for the same session stay in arrival order.
/// </summary>
public sealed class SessionWorkScheduler<TWork> : IAsyncDisposable
{
    private readonly ConcurrentDictionary<Guid, SessionQueue> _queues = new();
    private readonly ConcurrentDictionary<Guid, byte> _completedSessions = new();
    private readonly ConcurrentBag<Task> _runTasks = [];
    private readonly int _queueCapacity;
    private readonly Func<TWork, CancellationToken, Task> _dispatch;
    private readonly CancellationTokenSource _cts;
    private readonly ILogger? _logger;
    private int _disposed;

    public SessionWorkScheduler(
        int queueCapacity,
        Func<TWork, CancellationToken, Task> dispatch,
        CancellationToken ct,
        ILogger? logger = null
    )
    {
        ArgumentNullException.ThrowIfNull(dispatch);
        _queueCapacity = Math.Max(1, queueCapacity);
        _dispatch = dispatch;
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _logger = logger;
    }

    public bool TryEnqueue(Guid sessionId, TWork work)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);

        if (_completedSessions.ContainsKey(sessionId))
            return false;

        var queue = GetOrCreateQueue(sessionId);
        if (queue is null)
            return false;

        if (_completedSessions.ContainsKey(sessionId))
        {
            queue.Complete();
            _queues.TryRemove(sessionId, out _);
            return false;
        }

        return queue.TryEnqueue(work);
    }

    public void CompleteSession(Guid sessionId)
    {
        _completedSessions.TryAdd(sessionId, 0);
        if (_queues.TryRemove(sessionId, out var queue))
            queue.Complete();
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;

        try
        {
            await _cts.CancelAsync();
        }
        catch (ObjectDisposedException)
        {
            // already cancelled/disposed
        }

        foreach (var queue in _queues.Values)
            queue.Complete();

        try
        {
            await Task.WhenAll(_runTasks);
        }
        catch (OperationCanceledException)
        {
            // expected when the host token is cancelled
        }

        _queues.Clear();
        _completedSessions.Clear();
        _cts.Dispose();
        GC.SuppressFinalize(this);
    }

    private SessionQueue? GetOrCreateQueue(Guid sessionId)
    {
        if (_queues.TryGetValue(sessionId, out var existing))
            return existing;

        var created = new SessionQueue(_queueCapacity);
        if (_queues.TryAdd(sessionId, created))
        {
            var run = created.Start(_dispatch, _cts.Token, _logger);
            _runTasks.Add(run);
            return created;
        }

        created.Complete();
        return _queues.TryGetValue(sessionId, out existing) ? existing : null;
    }

    private sealed class SessionQueue
    {
        private readonly Channel<TWork> _channel;

        public SessionQueue(int capacity)
        {
            _channel = Channel.CreateBounded<TWork>(
                new BoundedChannelOptions(capacity)
                {
                    SingleReader = true,
                    SingleWriter = false,
                    FullMode = BoundedChannelFullMode.Wait,
                }
            );
        }

        public bool TryEnqueue(TWork work) => _channel.Writer.TryWrite(work);

        public void Complete() => _channel.Writer.TryComplete();

        public Task Start(
            Func<TWork, CancellationToken, Task> dispatch,
            CancellationToken ct,
            ILogger? logger
        ) => RunAsync(dispatch, ct, logger);

        private async Task RunAsync(
            Func<TWork, CancellationToken, Task> dispatch,
            CancellationToken ct,
            ILogger? logger
        )
        {
            try
            {
                await foreach (var work in _channel.Reader.ReadAllAsync(ct))
                {
                    try
                    {
                        await dispatch(work, ct);
                    }
                    catch (OperationCanceledException) when (ct.IsCancellationRequested)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        logger?.LogError(ex, "Session packet dispatch failed");
                    }
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                // host shutting down or listener restarting
            }
        }
    }
}
