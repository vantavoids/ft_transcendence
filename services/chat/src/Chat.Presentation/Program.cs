using System.Text.Json;
using Carter;
using Chat.Application;
using Chat.Infrastructure;
using Chat.Persistence;
using Chat.Presentation.Hubs;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Host.ConfigureHostOptions(o => o.ShutdownTimeout = TimeSpan.FromSeconds(5));

builder.Services.AddOpenApi();
builder.Services.AddHealthChecks()
	.AddPersistenceHealthChecks();
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
app.MapCarter();
app.MapHub<ChatHub>("/hubs/chat");
app.MapHub<SignalingHub>("/hubs/signaling");

app.Run();
