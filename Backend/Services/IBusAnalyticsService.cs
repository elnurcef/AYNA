using Backend.Models;

namespace Backend.Services;

public interface IBusAnalyticsService
{
    Task<PagedResult<BusAnalyticsItemDto>> GetAsync(
        BusAnalyticsQueryParameters query,
        CancellationToken cancellationToken);
}
