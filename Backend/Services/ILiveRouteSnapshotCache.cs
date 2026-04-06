using Backend.Models;

namespace Backend.Services;

public interface ILiveRouteSnapshotCache
{
    bool TryGetLatest(out LiveRouteSnapshot? snapshot);

    void SetLatest(LiveRouteSnapshot snapshot);
}
