using Carter;
using Guild.Application;
using Guild.Infrastructure;
using Guild.Persistence;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Host.ConfigureHostOptions(o => o.ShutdownTimeout = TimeSpan.FromSeconds(5));

builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();
builder.Services.AddApplication()
    .AddInfrastructure()
    .AddPersistence();
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