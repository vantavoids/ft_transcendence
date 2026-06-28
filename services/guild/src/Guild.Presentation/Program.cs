using System.Text;
using System.Text.Json;
using Carter;
using Guild.Application;
using Guild.Infrastructure;
using Guild.Persistence;
using Guild.Persistence.Db;
using Guild.Presentation.Endpoints;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
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
	.AddInfrastructure<GuildDbContext>()
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

app.UseExceptionHandler(exApp => exApp.Run(async ctx =>
{
	var feature = ctx.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
	var err = feature?.Error;

	if (err is BadHttpRequestException bad)
	{
		ctx.Response.StatusCode = bad.StatusCode;
		var jsonEx = bad.InnerException as System.Text.Json.JsonException;
		var path = jsonEx?.Path?.TrimStart('$', '.');
		var message = path is not null
			? $"invalid value for field '{path}'"
			: "bad request";
		await ctx.Response.WriteAsJsonAsync(new ErrorBody(message));
	}
	else
	{
		ctx.Response.StatusCode = 500;
		await ctx.Response.WriteAsJsonAsync(new ErrorBody("internal_server_error"));
	}
}));

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

// internal endpoints. the API Gateway only forwards /api/{service}/vN/...
// so /internal/... is unreachable from outside the docker network. callers
// (e.g. Chat Service) reach this directly via the compose service hostname.
// ExcludeFromDescription keeps these out of the OpenAPI doc + Scalar UI:
// Scalar is scoped to the gateway-facing server URL, where /internal/... is a
// 404. documenting them there would be misleading
var internalRoutes = app.MapGroup("/internal").ExcludeFromDescription();
ChannelMembershipEndpoint.MapInternalRoutes(internalRoutes);
OwnedGuildsCountEndpoint.MapInternalRoutes(internalRoutes);

app.Run();

public partial class Program;
