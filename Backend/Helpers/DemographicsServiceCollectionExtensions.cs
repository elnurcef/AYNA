using Backend.Services;

namespace Backend.Helpers;

public static class DemographicsServiceCollectionExtensions
{
    public static IServiceCollection AddDemographics(this IServiceCollection services)
    {
        services.AddSingleton<IDemographicsService>(serviceProvider =>
        {
            var environment = serviceProvider.GetRequiredService<IHostEnvironment>();
            var logger = serviceProvider.GetRequiredService<ILogger<DemographicsService>>();
            var geoPackagePath = ResolveGeoPackagePath(environment.ContentRootPath);

            return new DemographicsService(geoPackagePath, logger);
        });

        return services;
    }

    private static string ResolveGeoPackagePath(string contentRootPath)
    {
        var dataDirectory = Path.GetFullPath(Path.Combine(contentRootPath, "Data"));

        var candidates = new[]
        {
            Path.Combine(dataDirectory, "zone_attributes_synthetic.gpkg"),
            Path.Combine(dataDirectory, "zone_attributes_synthetic .gpkg")
        };

        foreach (var candidate in candidates)
        {
            if (!File.Exists(candidate))
            {
                continue;
            }

            if (LooksLikeGeoPackage(candidate))
            {
                return candidate;
            }
        }

        throw new FileNotFoundException(
            "A valid demographics GeoPackage file could not be found in the Data directory.",
            Path.Combine(dataDirectory, "zone_attributes_synthetic.gpkg"));
    }

    private static bool LooksLikeGeoPackage(string path)
    {
        try
        {
            using var connection = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={path};Mode=ReadOnly");
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = 'gpkg_contents'";

            return Convert.ToInt32(command.ExecuteScalar()) > 0;
        }
        catch
        {
            return false;
        }
    }
}
