using Backend.Models;

namespace Backend.Services;

public sealed class LiveRouteSnapshotCache : ILiveRouteSnapshotCache
{
    private readonly object _syncRoot = new();
    private LiveRouteSnapshot? _latestSnapshot;

    public bool TryGetLatest(out LiveRouteSnapshot? snapshot)
    {
        lock (_syncRoot)
        {
            snapshot = _latestSnapshot;
            return snapshot is not null;
        }
    }

    public void SetLatest(LiveRouteSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        lock (_syncRoot)
        {
            _latestSnapshot = snapshot;
        }
    }
}
