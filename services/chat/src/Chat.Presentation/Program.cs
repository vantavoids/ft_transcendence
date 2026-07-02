using System.Text;
using System.Text.Json;
using Carter;
using Chat.Application;
using Chat.Application.Abstractions;
using Chat.Infrastructure;
using Chat.Persistence;
using Chat.Presentation.Endpoints;
using Chat.Presentation.Hubs;
using Chat.Presentation.Hubs.Signaling;
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
builder.Services.AddSingleton<UserConnectionTracker>();
builder.Services.AddSingleton<CallRegistry>();
builder.Services.Configure<SignalingOptions>(builder.Configuration.GetSection(SignalingOptions.SectionName));
builder.Services.AddScoped<IChannelBroadcaster, SignalRChannelBroadcaster>();
builder.Services.AddScoped<IUserBroadcaster, SignalRUserBroadcaster>();
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
				if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/v1/hubs"))
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
var v1 = app.MapGroup("/v1").RequireAuthorization();
v1.MapCarter();
app.MapHub<ChatHub>("/v1/hubs/chat");
app.MapHub<SignalingHub>("/v1/hubs/signaling");

// internal endpoints: the API Gateway only forwards /api/{service}/vN/..., so
// /internal/... is reachable only over the docker network (no auth header).
var internalRoutes = app.MapGroup("/internal");
UserDataExportEndpoint.MapInternalRoutes(internalRoutes);

app.Run();

public partial class Program;
