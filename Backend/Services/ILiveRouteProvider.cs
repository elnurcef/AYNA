using Backend.Models;

namespace Backend.Services;

public interface ILiveRouteProvider
{
    string Name { get; }

    Task<LiveRouteSnapshot> GetLiveRoutesAsync(CancellationToken cancellationToken);
}
