using Carter;
using Auth.Application;
using Auth.Infrastructure;
using Auth.Persistence;
using Auth.Presentation.Middleware;
using Auth.Presentation;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
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
var v1 = app.MapGroup("/v1");
v1.MapCarter();

app.Run();