using Backend.Models;
using NetTopologySuite.Features;

namespace Backend.Services;

public interface IDemographicsService
{
    Task<FeatureCollection> GetFeatureCollectionAsync(
        DemographicsQueryParameters query,
        CancellationToken cancellationToken);
}
