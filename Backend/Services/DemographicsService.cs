using Backend.Helpers;
using Backend.Models;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.Geometries.Utilities;
using NetTopologySuite.Operation.Union;

namespace Backend.Services;

public sealed class DemographicsService : IDemographicsService
{
    private readonly IReadOnlyDictionary<string, IReadOnlyList<DemographicsAggregate>> _aggregatesByLevel;

    public DemographicsService(
        string geoPackagePath,
        ILogger<DemographicsService> logger)
    {
        var features = DemographicsGeoPackageReader.ReadFeatures(geoPackagePath);

        _aggregatesByLevel = new Dictionary<string, IReadOnlyList<DemographicsAggregate>>(StringComparer.OrdinalIgnoreCase)
        {
            ["micro"] = BuildAggregates(features, feature => feature.Micro),
            ["meso"] = BuildAggregates(features, feature => feature.Meso),
            ["macro"] = BuildAggregates(features, feature => feature.Macro)
        };

        logger.LogInformation(
            "Loaded {FeatureCount} demographics features from {GeoPackagePath}",
            features.Count,
            geoPackagePath);
    }

    public Task<FeatureCollection> GetFeatureCollectionAsync(
        DemographicsQueryParameters query,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var aggregates = _aggregatesByLevel[query.NormalizedLevel];
        var featureCollection = new FeatureCollection();

        foreach (var aggregate in aggregates)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var attributes = new AttributesTable
            {
                { "regionName", aggregate.RegionName },
                { "value", query.NormalizedMetric == "jobs" ? aggregate.Jobs : aggregate.Population }
            };

            featureCollection.Add(new Feature(aggregate.Geometry, attributes));
        }

        return Task.FromResult(featureCollection);
    }

    private static IReadOnlyList<DemographicsAggregate> BuildAggregates(
        IReadOnlyCollection<DemographicsGeoPackageFeature> features,
        Func<DemographicsGeoPackageFeature, string> regionSelector)
    {
        return features
            .GroupBy(regionSelector, StringComparer.Ordinal)
            .Select(group =>
            {
                var geometries = group
                    .Select(feature => feature.Geometry)
                    .ToArray();

                var dissolvedGeometry = DissolveGeometries(geometries);

                return new DemographicsAggregate(
                    RegionName: group.Key,
                    Population: group.Sum(feature => feature.Population),
                    Jobs: group.Sum(feature => feature.Jobs),
                    Geometry: dissolvedGeometry);
            })
            .OrderBy(aggregate => aggregate.RegionName, NaturalRegionNameComparer.Instance)
            .ToArray();
    }

    private static Geometry DissolveGeometries(IReadOnlyCollection<Geometry> geometries)
    {
        if (geometries.Count == 1)
        {
            return GeometryFixer.Fix(geometries.Single());
        }

        var fixedGeometries = geometries
            .Select(GeometryFixer.Fix)
            .ToArray();

        return UnaryUnionOp.Union(fixedGeometries);
    }
}
