using System.Collections.Concurrent;

namespace AISpace.Common.Game;

public sealed class PendingTransitionStore : IPendingTransitionStore
{
    private readonly ConcurrentDictionary<
        int,
        SharedState.PendingMapTransfer
    > _pendingAreaTransitionsByUserId = new();

    public void SetPendingAreaTransition(SharedState.PendingMapTransfer transition) =>
        _pendingAreaTransitionsByUserId[transition.UserId] = transition;

    public bool TryTakePendingAreaTransition(
        int userId,
        out SharedState.PendingMapTransfer transition
    ) => _pendingAreaTransitionsByUserId.TryRemove(userId, out transition);
}
