using Backend.BackgroundWorkers;
using Backend.Endpoints;
using Backend.Helpers;
using Backend.Hubs;
using Backend.Services;
using NetTopologySuite.IO.Converters;

var builder = WebApplication.CreateBuilder(args);

const string AngularDevCorsPolicy = "AngularDevCors";

var angularOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? ["http://localhost:4200"];

builder.Services.AddCors(options =>
{
    options.AddPolicy(AngularDevCorsPolicy, policy =>
    {
        policy.WithOrigins(angularOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddSignalR();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new GeoJsonConverterFactory());
});

builder.Services.AddDemographics();
builder.Services.AddBusAnalytics();
builder.Services.AddLiveRoutes(builder.Configuration);
builder.Services.AddHostedService<RoutesPollingWorker>();

var app = builder.Build();

_ = app.Services.GetRequiredService<IDemographicsService>();
_ = app.Services.GetRequiredService<IBusAnalyticsService>();

app.UseCors(AngularDevCorsPolicy);

app.MapGet("/", () => Results.Ok(new
{
    service = "AYNA Backend",
    endpoints = new[]
    {
        "/api/demographics",
        "/api/bus-analytics",
        "/api/routes/live",
        "/hubs/routes"
    }
}));

app.MapDemographicsEndpoints();
app.MapBusAnalyticsEndpoints();
app.MapRouteLiveEndpoints();
app.MapHub<RoutesHub>("/hubs/routes");

app.Run();
