using Backend.Models;

namespace Backend.Services;

public interface IRouteLiveService
{
    Task<LiveRouteSnapshot> GetCurrentSnapshotAsync(CancellationToken cancellationToken);
    Task<LiveRouteSnapshot> RefreshAsync(CancellationToken cancellationToken);
}
