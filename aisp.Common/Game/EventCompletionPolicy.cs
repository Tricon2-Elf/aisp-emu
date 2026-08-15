namespace aisp.Common.Game;

/// <summary>
/// Whether finishing an event should persist completion in <c>CharacterEventStatuses</c>.
/// Session activity (<see cref="IPlayerSession.ActiveEventKey"/>) is tracked regardless.
/// </summary>
public enum EventCompletionPolicy : byte
{
    /// <summary>Mark the event completed when it finishes successfully.</summary>
    Once = 0,

    /// <summary>Allow repeating; never write a completion row for this run.</summary>
    Replayable = 1,
}
