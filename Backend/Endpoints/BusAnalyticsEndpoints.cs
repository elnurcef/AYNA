using Backend.Models;
using Backend.Services;

namespace Backend.Endpoints;

public static class BusAnalyticsEndpoints
{
    public static IEndpointRouteBuilder MapBusAnalyticsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/bus-analytics")
            .WithTags("Bus Analytics");

        group.MapGet("/", GetBusAnalyticsAsync);

        return endpoints;
    }

    private static async Task<IResult> GetBusAnalyticsAsync(
        [AsParameters] BusAnalyticsQueryParameters query,
        IBusAnalyticsService analyticsService,
        CancellationToken cancellationToken)
    {
        var validationErrors = ValidateQuery(query);

        if (validationErrors.Count > 0)
        {
            return Results.ValidationProblem(validationErrors);
        }

        var response = await analyticsService.GetAsync(query, cancellationToken);
        return Results.Ok(response);
    }

    private static Dictionary<string, string[]> ValidateQuery(BusAnalyticsQueryParameters query)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

        if (query.Page < 1)
        {
            errors["page"] = ["The page query parameter must be greater than or equal to 1."];
        }

        if (query.PageSize < 1)
        {
            errors["pageSize"] = ["The pageSize query parameter must be greater than or equal to 1."];
        }

        return errors;
    }
}
