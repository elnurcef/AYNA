using NetTopologySuite.Geometries;

namespace Backend.Models;

public sealed record DemographicsAggregate(
    string RegionName,
    int Population,
    int Jobs,
    Geometry Geometry);
