using Carter;
using Auth.Application;
using Auth.Infrastructure;
using Auth.Persistence;
using Auth.Presentation.Endpoints;
using Auth.Presentation.Middleware;
using Auth.Presentation;
using Auth.Presentation.Observability;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using Scalar.AspNetCore;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Host.ConfigureHostOptions(o => o.ShutdownTimeout = TimeSpan.FromSeconds(5));
builder.Services.ConfigureHttpJsonOptions(o =>
                    o.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
                );

builder.Services.AddOpenApi();
builder.Services.AddHealthChecks()
                .AddPersistenceHealthChecks();

builder.Services.AddApplication()
                .AddInfrastructure()
                .AddPersistence()
                .AddJwtAuthentication();

// metrics for Prometheus to scrape at /metrics. AspNetCore instrumentation emits
// the semconv http.server.* RED metrics shared across the fleet; Runtime adds
// GC/threadpool gauges; the Auth.Domain meter adds business gauges (accounts,
// OAuth adoption, active sessions). see docs/monitoring-metrics.md.
builder.Services.AddSingleton<AuthMetrics>();
builder.Services.AddHostedService<AuthMetricsCollector>();
builder.Services.AddOpenTelemetry()
                .ConfigureResource(r => r.AddService("auth"))
                .WithMetrics(m => m
                    .AddAspNetCoreInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddMeter(AuthMetrics.MeterName)
                    .AddPrometheusExporter());

builder.Services.AddCarter();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference((options, ctx) =>
    {
        var apiUrl = Environment.GetEnvironmentVariable("BASE_API_URL");
        options.AddServer(new ScalarServer($"{apiUrl}/auth"));
    });
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/healthz", new HealthCheckOptions
{
    ResponseWriter = static async (ctx, report) =>
    {
        ctx.Response.ContentType = "application/json";
        await ctx.Response.WriteAsync(JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
            }),
        }));
    },
});
// Prometheus scrape endpoint (GET /metrics), anonymous + internal-only like /healthz.
app.MapPrometheusScrapingEndpoint();

var v1 = app.MapGroup("/v1");
v1.MapCarter();

// internal endpoints. the API Gateway only forwards /api/{service}/vN/...
// so /internal/... is unreachable from outside the docker network.
var internalRoutes = app.MapGroup("/internal").ExcludeFromDescription();
UserDataExportEndpoint.MapInternalRoutes(internalRoutes);

app.Run();