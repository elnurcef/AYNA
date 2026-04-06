using NetTopologySuite.Geometries;

namespace Backend.Models;

public sealed record DemographicsGeoPackageFeature(
    string Micro,
    string Meso,
    string Macro,
    int Population,
    int Jobs,
    Geometry Geometry);
