using Carter;
using Auth.Application;
using Auth.Infrastructure;
using Auth.Persistence;
using Scalar.AspNetCore;

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
}

app.MapHealthChecks("/healthz");
app.MapCarter();

app.Run();