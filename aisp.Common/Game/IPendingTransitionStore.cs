namespace aisp.Common.Game;

public interface IPendingTransitionStore
{
    void SetPendingAreaTransition(SharedState.PendingMapTransfer transition);

    bool TryTakePendingAreaTransition(int userId, out SharedState.PendingMapTransfer transition);
}
