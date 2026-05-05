using Carter;
using Auth.Application;
using Auth.Infrastructure;
using Auth.Persistence;
using Auth.Presentation.Middleware;
using Auth.Presentation;
using Scalar.AspNetCore;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Host.ConfigureHostOptions(o => o.ShutdownTimeout = TimeSpan.FromSeconds(5));
builder.Services.ConfigureHttpJsonOptions(o =>
                    o.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
                );

builder.Services.AddOpenApi()
                .AddHealthChecks();

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

app.MapHealthChecks("/healthz");
var v1 = app.MapGroup("/v1");
v1.MapCarter();

app.Run();