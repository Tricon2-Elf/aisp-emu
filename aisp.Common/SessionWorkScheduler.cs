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
    private readonly ConcurrentDictionary<Guid, SessionState> _sessions = new();
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

    internal int TrackedSessionCount => _sessions.Count;

    /// <summary>
    /// Enqueues work for <paramref name="sessionId"/>, waiting if that session's queue is at capacity.
    /// Returns false if the session is already completed or the scheduler is disposing.
    /// </summary>
    public async ValueTask<bool> EnqueueAsync(
        Guid sessionId,
        TWork work,
        CancellationToken ct = default
    )
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);

        var state = _sessions.GetOrAdd(sessionId, static _ => new SessionState());
        ChannelWriter<TWork> writer;
        lock (state.Gate)
        {
            if (state.Completed || Volatile.Read(ref _disposed) != 0)
                return false;

            if (state.Queue is null)
            {
                state.Queue = new SessionQueue(_queueCapacity);
                state.Runner = state.Queue.Start(_dispatch, _cts.Token, _logger);
                ObserveRunner(sessionId, state, state.Runner);
            }

            writer = state.Queue.Writer;
        }

        try
        {
            await writer.WriteAsync(work, ct).ConfigureAwait(false);
            return true;
        }
        catch (ChannelClosedException)
        {
            return false;
        }
    }

    public void CompleteSession(Guid sessionId)
    {
        if (!_sessions.TryGetValue(sessionId, out var state))
            return;

        lock (state.Gate)
        {
            state.Completed = true;
            state.Queue?.Complete();
            if (state.Runner is null)
                _sessions.TryRemove(sessionId, out _);
        }
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

        List<Task> runners = [];
        foreach (var state in _sessions.Values)
        {
            lock (state.Gate)
            {
                state.Completed = true;
                state.Queue?.Complete();
                if (state.Runner is not null)
                    runners.Add(state.Runner);
            }
        }

        try
        {
            await Task.WhenAll(runners);
        }
        catch (OperationCanceledException)
        {
            // expected when the host token is cancelled
        }

        _sessions.Clear();
        _cts.Dispose();
        GC.SuppressFinalize(this);
    }

    private void ObserveRunner(Guid sessionId, SessionState state, Task runner)
    {
        _ = ForgetRunnerWhenCompleteAsync(sessionId, state, runner);
    }

    private async Task ForgetRunnerWhenCompleteAsync(
        Guid sessionId,
        SessionState state,
        Task runner
    )
    {
        try
        {
            await runner.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // host shutting down or listener restarting
        }
        finally
        {
            lock (state.Gate)
            {
                if (ReferenceEquals(state.Runner, runner))
                {
                    state.Runner = null;
                    state.Queue = null;
                    if (state.Completed)
                        _sessions.TryRemove(sessionId, out _);
                }
            }
        }
    }

    private sealed class SessionState
    {
        public readonly object Gate = new();
        public SessionQueue? Queue;
        public Task? Runner;
        public bool Completed;
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

        public ChannelWriter<TWork> Writer => _channel.Writer;

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
