using Backend.Models;
using Microsoft.Extensions.Options;

namespace Backend.Services;

public sealed class RouteLiveService : IRouteLiveService
{
    private readonly ILiveRouteSnapshotCache _snapshotCache;
    private readonly IReadOnlyDictionary<string, ILiveRouteProvider> _providers;
    private readonly LiveRoutesOptions _options;
    private readonly ILogger<RouteLiveService> _logger;

    public RouteLiveService(
        ILiveRouteSnapshotCache snapshotCache,
        IEnumerable<ILiveRouteProvider> providers,
        IOptions<LiveRoutesOptions> options,
        ILogger<RouteLiveService> logger)
    {
        _snapshotCache = snapshotCache;
        _options = options.Value;
        _logger = logger;
        _providers = providers.ToDictionary(
            provider => provider.Name,
            StringComparer.OrdinalIgnoreCase);
    }

    public Task<LiveRouteSnapshot> GetCurrentSnapshotAsync(CancellationToken cancellationToken)
    {
        if (_snapshotCache.TryGetLatest(out var snapshot) &&
            snapshot is not null)
        {
            return Task.FromResult(snapshot);
        }

        return RefreshAsync(cancellationToken);
    }

    public async Task<LiveRouteSnapshot> RefreshAsync(CancellationToken cancellationToken)
    {
        var failures = new List<string>();

        foreach (var provider in GetProviderSequence())
        {
            try
            {
                var snapshot = await provider.GetLiveRoutesAsync(cancellationToken);

                _snapshotCache.SetLatest(snapshot);

                return snapshot;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "Live route provider {Provider} failed.",
                    provider.Name);

                failures.Add($"{provider.Name}: {exception.Message}");
            }
        }

        throw new LiveRouteProviderException(
            $"Unable to retrieve live route data. {string.Join(" | ", failures)}");
    }

    private IEnumerable<ILiveRouteProvider> GetProviderSequence()
    {
        if (string.Equals(_options.ProviderMode, "http", StringComparison.OrdinalIgnoreCase))
        {
            return GetNamedProviders(["http"]);
        }

        if (string.Equals(_options.ProviderMode, "playwright", StringComparison.OrdinalIgnoreCase))
        {
            return GetNamedProviders(["playwright"]);
        }

        return GetNamedProviders(_options.ProviderOrder);
    }

    private IEnumerable<ILiveRouteProvider> GetNamedProviders(IEnumerable<string> providerNames)
    {
        foreach (var providerName in providerNames)
        {
            if (_providers.TryGetValue(providerName, out var provider))
            {
                yield return provider;
            }
        }
    }
}
