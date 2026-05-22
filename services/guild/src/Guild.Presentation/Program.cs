using System.Text;
using System.Text.Json;
using Carter;
using Guild.Application;
using Guild.Infrastructure;
using Guild.Persistence;
using Guild.Presentation.Endpoints;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Host.ConfigureHostOptions(o => o.ShutdownTimeout = TimeSpan.FromSeconds(5));

builder.Services.ConfigureHttpJsonOptions(o =>
	o.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower);

builder.Services.AddOpenApi();
builder.Services.AddHealthChecks()
	.AddPersistenceHealthChecks();

builder.Services.AddApplication()
	.AddInfrastructure()
	.AddPersistence();

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
	});
builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
	app.MapOpenApi();
	app.MapScalarApiReference((options, ctx) =>
	{
		var apiUrl = Environment.GetEnvironmentVariable("BASE_API_URL");
		options.AddServer(new ScalarServer($"{apiUrl}/guild"));
	});
}

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/healthz");

var v1 = app.MapGroup("/v1").RequireAuthorization();
v1.MapCarter();

// internal endpoints. the API Gateway only forwards /api/{service}/vN/...
// so /internal/... is unreachable from outside the docker network. callers
// (e.g. Chat Service) reach this directly via the compose service hostname
var internalRoutes = app.MapGroup("/internal");
ChannelMembershipEndpoint.MapInternalRoutes(internalRoutes);

app.Run();

public partial class Program;
