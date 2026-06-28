using Guild.Application.Abstractions.Persistence;
using Guild.Persistence.Db;
using Guild.Persistence.Health;
using Guild.Persistence.Maintenance;
using Guild.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Guild.Persistence;

public static class DependencyInjection
{
	public static IServiceCollection AddPersistence(this IServiceCollection services)
	{
		services.AddOptions<DbOptions>()
			.BindConfiguration(DbOptions.SectionName)
			.ValidateDataAnnotations()
			.ValidateOnStart();

		services.AddDbContext<GuildDbContext>((ctx, config) =>
		{
			var options = ctx.GetRequiredService<IOptions<DbOptions>>().Value;
			config.UseNpgsql(options.ToConnectionString(), o => o.MapPostgresEnums());
		});

		services.AddScoped<IUnitOfWork, UnitOfWork>();
		services.AddScoped<IGuildRepository, GuildRepository>();
		services.AddScoped<IGuildInviteRepository, GuildInviteRepository>();
		services.AddScoped<IGuildBanRepository, GuildBanRepository>();
		services.AddScoped<IChannelCategoryRepository, ChannelCategoryRepository>();
		services.AddScoped<IChannelRepository, ChannelRepository>();
		services.AddScoped<IChannelPermissionOverwriteRepository, ChannelPermissionOverwriteRepository>();

		services.AddOptions<InviteCleanupOptions>()
			.BindConfiguration(InviteCleanupOptions.SectionName)
			.ValidateDataAnnotations()
			.ValidateOnStart();
		services.AddHostedService<InviteCleanupService>();

		return services;
	}

	public static IHealthChecksBuilder AddPersistenceHealthChecks(this IHealthChecksBuilder builder)
		=> builder.AddCheck<EfCoreHealthCheck>("postgres");
}
