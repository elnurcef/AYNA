using Backend.Services;

namespace Backend.Helpers;

public static class BusAnalyticsServiceCollectionExtensions
{
    public static IServiceCollection AddBusAnalytics(this IServiceCollection services)
    {
        services.AddSingleton<IBusAnalyticsService>(serviceProvider =>
        {
            var environment = serviceProvider.GetRequiredService<IHostEnvironment>();
            var logger = serviceProvider.GetRequiredService<ILogger<BusAnalyticsService>>();
            var csvFilePath = Path.GetFullPath(Path.Combine(
                environment.ContentRootPath,
                "Data",
                "ceck_in_buss.csv"));

            return new BusAnalyticsService(csvFilePath, logger);
        });

        return services;
    }
}
