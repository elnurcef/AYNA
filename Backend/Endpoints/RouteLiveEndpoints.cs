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
        var response = await routeLiveService.GetCurrentSnapshotAsync(cancellationToken);
        return Results.Ok(response);
    }
}
