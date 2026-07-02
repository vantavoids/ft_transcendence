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
using Guild.Presentation.Observability;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Host.ConfigureHostOptions(o => o.ShutdownTimeout = TimeSpan.FromSeconds(5));

builder.Services.ConfigureHttpJsonOptions(o =>
	o.SerializerOptions.PropertyNamingPolicy = GuildSerialization.NamingPolicy);

builder.Services.AddOpenApi();
builder.Services.AddHealthChecks()
	.AddPersistenceHealthChecks();

// metrics for Prometheus to scrape at /metrics. AspNetCore instrumentation emits
// the semconv http.server.* RED metrics shared across the fleet; Runtime adds
// GC/threadpool gauges; the Guild.Domain meter adds business gauges (guild/member/
// channel/role/invite/ban totals). see docs/monitoring-metrics.md for the contract.
builder.Services.AddSingleton<GuildMetrics>();
builder.Services.AddHostedService<GuildMetricsCollector>();
builder.Services.AddOpenTelemetry()
	.ConfigureResource(r => r.AddService("guild"))
	.WithMetrics(m => m
		.AddAspNetCoreInstrumentation()
		.AddRuntimeInstrumentation()
		.AddMeter(GuildMetrics.MeterName)
		.AddPrometheusExporter());

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
		var jsonEx = bad.InnerException as JsonException;
		var path = jsonEx?.Path?.TrimStart('$', '.');
		var message = path is not null
			? $"invalid value for field '{path}'"
			: "bad request";
		await ctx.Response.WriteAsJsonAsync(new ErrorBody(message));
	}
	else if (err is Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException)
	{
		// optimistic-concurrency conflict: another request modified the same row
		// version between our read and write (xmin token, see the entity configs).
		// surface a retryable 409 instead of a 500 so the client can re-fetch and
		// retry rather than treating it as a server fault
		ctx.Response.StatusCode = StatusCodes.Status409Conflict;
		await ctx.Response.WriteAsJsonAsync(
			new ErrorBody("the resource was modified by another request; please retry"));
	}
	else if (err is Microsoft.EntityFrameworkCore.DbUpdateException
		{ InnerException: Npgsql.PostgresException { SqlState: Npgsql.PostgresErrorCodes.UniqueViolation } })
	{
		// lost a race to insert a row with a unique/PK that already exists - e.g.
		// two concurrent double-bans both pass the FindAsync guard then both INSERT
		// on guild_bans' composite PK. xmin does not cover inserts, so EF raises a
		// plain DbUpdateException (not the concurrency variant above). 23505 is a
		// genuine conflict, so map it to 409 rather than letting it fall to a 500.
		ctx.Response.StatusCode = StatusCodes.Status409Conflict;
		await ctx.Response.WriteAsJsonAsync(
			new ErrorBody("the resource already exists"));
	}
	else
	{
		// genuinely unexpected: log it (with the exception) before returning an
		// opaque 500. previously the error was read and silently swallowed, so
		// production failures left no trace.
		var logger = ctx.RequestServices
			.GetRequiredService<ILoggerFactory>()
			.CreateLogger("Guild.GlobalExceptionHandler");
		logger.LogError(err, "Unhandled exception processing {Method} {Path}",
			ctx.Request.Method, ctx.Request.Path);

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

// Prometheus scrape endpoint (GET /metrics). anonymous + internal-only: the
// gateway only forwards /api/{service}/vN/..., so /metrics is reachable solely
// over the docker network, like /healthz.
app.MapPrometheusScrapingEndpoint();

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
UserDataExportEndpoint.MapInternalRoutes(internalRoutes);

app.Run();

public partial class Program;
