using Backend.Models;
using Backend.Services;

namespace Backend.Endpoints;

public static class RouteLiveEndpoints
{
    public static IEndpointRouteBuilder MapRouteLiveEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/routes/live")
            .WithTags("Routes");

        group.MapGet("/", GetLiveRoutesAsync);

        return endpoints;
    }

    private static async Task<IResult> GetLiveRoutesAsync(
        IRouteLiveService routeLiveService,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await routeLiveService.GetCurrentSnapshotAsync(cancellationToken);
            return Results.Ok(response);
        }
        catch (LiveRouteProviderException exception)
        {
            return Results.Problem(
                title: "Live routes are unavailable.",
                detail: exception.Message,
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }
    }
}
