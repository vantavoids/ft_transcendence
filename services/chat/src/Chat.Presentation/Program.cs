using Carter;
using Chat.Application;
using Chat.Infrastructure;
using Chat.Persistence;
using Chat.Presentation.Hubs;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();
builder.Services.AddSignalR();
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
app.MapHub<ChatHub>("/hubs/chat");

app.Run();
