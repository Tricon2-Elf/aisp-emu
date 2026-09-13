namespace aisp.Common.Services.Toxicity;

/// <summary>
/// Fire-and-forget enqueue for chat toxicity classification. Never blocks the caller.
/// </summary>
public interface IChatToxicityClassifier
{
    /// <summary>
    /// Queues a persisted chat row for classification. Returns false if the queue is full
    /// or classification is disabled.
    /// </summary>
    bool TryEnqueue(long id, string message);
}

/// <summary>No-op when ChatToxicity is disabled or the worker failed to start.</summary>
public sealed class NullChatToxicityClassifier : IChatToxicityClassifier
{
    public static NullChatToxicityClassifier Instance { get; } = new();

    public bool TryEnqueue(long id, string message) => false;
}
