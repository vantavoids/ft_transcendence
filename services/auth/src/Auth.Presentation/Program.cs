using Carter;
using Auth.Application;
using Auth.Infrastructure;
using Auth.Persistence;
using Scalar.AspNetCore;
using Auth.Persistence.Db;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Host.ConfigureHostOptions(o => o.ShutdownTimeout = TimeSpan.FromSeconds(5));

builder.Services.AddOpenApi()
                .AddHealthChecks();

builder.Services.AddApplication()
                .AddInfrastructure()
                .AddPersistence(builder.Configuration);

builder.Services.AddCarter();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();

    app.MapGet("/auth/test/dbConnection", async (AuthDbContext db) => 
    {
        await db.Database.OpenConnectionAsync();
        await db.Database.CloseConnectionAsync();

        return TypedResults.Ok("Connected to PostgreSQL!");
    });
}

app.MapHealthChecks("/healthz");
app.MapCarter();

app.Run();