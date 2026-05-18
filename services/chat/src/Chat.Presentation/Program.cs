using System.Text;
using System.Text.Json;
using Carter;
using Chat.Application;
using Chat.Application.Abstractions;
using Chat.Infrastructure;
using Chat.Persistence;
using Chat.Presentation.Hubs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Host.ConfigureHostOptions(o => o.ShutdownTimeout = TimeSpan.FromSeconds(5));

builder.Services.ConfigureHttpJsonOptions(o =>
	o.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower);

builder.Services.AddOpenApi();
builder.Services.AddHealthChecks()
	.AddPersistenceHealthChecks();
builder.Services.AddSignalR();
builder.Services.AddApplication()
	.AddInfrastructure()
	.AddPersistence();
builder.Services.AddScoped<IChannelBroadcaster, SignalRChannelBroadcaster>();
builder.Services.AddCarter();

var jwtSecret = builder.Configuration["Jwt:SecretKey"]
	?? throw new InvalidOperationException("Jwt:SecretKey is not configured.");

builder.Services
	.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
	.AddJwtBearer(options =>
	{
		options.TokenValidationParameters = new TokenValidationParameters
		{
			ValidateIssuerSigningKey = true,
			IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
			ValidateLifetime = true,
			ValidateIssuer = false,
			ValidateAudience = false,
		};
		// SignalR sends the JWT as ?access_token=... on the WebSocket upgrade
		// request because browsers can't attach custom headers to WS handshakes
		// bad browsers, time to switch to raw sockets! /j
		options.Events = new JwtBearerEvents
		{
			OnMessageReceived = ctx =>
			{
				var accessToken = ctx.Request.Query["access_token"];
				var path = ctx.HttpContext.Request.Path;
				if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
					ctx.Token = accessToken;
				return Task.CompletedTask;
			},
		};
	});
builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
	app.MapOpenApi();
	app.MapScalarApiReference();
}

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
app.MapCarter();
app.MapHub<ChatHub>("/hubs/chat");
app.MapHub<SignalingHub>("/hubs/signaling");

app.Run();

public partial class Program;
