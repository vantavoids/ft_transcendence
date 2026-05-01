using Carter;
using Auth.Application;
using Auth.Infrastructure;
using Auth.Persistence;
using Scalar.AspNetCore;
using Auth.Persistence.Db;
using Microsoft.EntityFrameworkCore;
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
                .AddPersistence();

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

    app.MapGet("/auth/test/dbConnection", async (AuthDbContext db) => 
    {
        await db.Database.OpenConnectionAsync();
        await db.Database.CloseConnectionAsync();

        return TypedResults.Ok("Connected to PostgreSQL!");
    });
}

app.MapHealthChecks("/healthz");
var v1 = app.MapGroup("/v1");
v1.MapCarter();

app.Run();