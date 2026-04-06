using Backend.Models;
using Backend.Services;

namespace Backend.Endpoints;

public static class DemographicsEndpoints
{
    public static IEndpointRouteBuilder MapDemographicsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/demographics")
            .WithTags("Demographics");

        group.MapGet("/", GetDemographicsAsync);

        return endpoints;
    }

    private static async Task<IResult> GetDemographicsAsync(
        [AsParameters] DemographicsQueryParameters query,
        IDemographicsService demographicsService,
        CancellationToken cancellationToken)
    {
        var validationErrors = ValidateQuery(query);

        if (validationErrors.Count > 0)
        {
            return Results.ValidationProblem(validationErrors);
        }

        var response = await demographicsService.GetFeatureCollectionAsync(query, cancellationToken);
        return Results.Ok(response);
    }

    private static Dictionary<string, string[]> ValidateQuery(DemographicsQueryParameters query)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

        if (!DemographicsQueryParameters.IsSupportedLevel(query.Level))
        {
            errors["level"] = ["The level query parameter must be one of: micro, meso, macro."];
        }

        if (!DemographicsQueryParameters.IsSupportedMetric(query.Metric))
        {
            errors["metric"] = ["The metric query parameter must be one of: population, jobs."];
        }

        return errors;
    }
}
