using System.Reflection;
using System.Text.Json;
using Guild.Application;
using Guild.Application.Abstractions;
using Guild.Application.Abstractions.Security;
using Guild.Application.Abstractions.Users;
using Guild.Application.Contracts;
using Guild.Infrastructure.Messaging;
using Guild.Infrastructure.Options;
using Guild.Infrastructure.Security;
using Guild.Infrastructure.Users;
using MassTransit;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Guild.Infrastructure;

public static class DependencyInjection
{
	public static IServiceCollection AddInfrastructure<TDbContext>(this IServiceCollection services)
		where TDbContext : DbContext
	{
		var optionsTypes = typeof(DependencyInjection).Assembly.GetTypes()
			.Where(t => t is { IsAbstract: false, IsInterface: false } &&
						typeof(IOptions).IsAssignableFrom(t));

		foreach (var optionType in optionsTypes)
		{
			var configureOptionsMethod = typeof(DependencyInjection)
				.GetMethod("ConfigureOptions", BindingFlags.NonPublic | BindingFlags.Static)!
				.MakeGenericMethod(optionType);

			configureOptionsMethod.Invoke(null, [services]);
		}

		services.AddMassTransit(x =>
		{
			// transactional bus outbox: a published event is written to the
			// OutboxMessage table inside the same SaveChanges as the business
			// change (so a broker outage can no longer drop an event), then the
			// delivery service ships it to RabbitMQ after the commit. TDbContext
			// is supplied by the composition root so Infrastructure never has to
			// reference the Persistence project (LayerDependencyTests forbids it).
			x.AddEntityFrameworkOutbox<TDbContext>(o =>
			{
				o.UsePostgres();
				o.UseBusOutbox();
			});

			x.UsingRabbitMq((ctx, cfg) =>
			{
				var options = ctx.GetRequiredService<IOptions<RabbitMqOptions>>().Value;
				cfg.Host(options.Host, options.VirtualHost, h =>
				{
					h.Username(options.Username);
					h.Password(options.Password);
				});

				// the docs/contracts source of truth specify snake_case payloads.
				// MassTransit defaults to camelCase via System.Text.Json, so override
				// the property naming policy so the wire format matches the docs.
				// every other service that publishes or consumes these events must
				// apply the same policy or deserialisation will fail
				cfg.ConfigureJsonSerializerOptions(opts =>
				{
					opts.PropertyNamingPolicy = GuildSerialization.NamingPolicy;
					return opts;
				});

				cfg.Message<GuildMemberJoined>(m => m.SetEntityName("guild.member_joined"));
				cfg.Message<GuildMemberLeft>(m => m.SetEntityName("guild.member_left"));
				cfg.Message<GuildInviteCreated>(m => m.SetEntityName("guild.invite_created"));

				cfg.ConfigureEndpoints(ctx);
			});
		});

		services.AddHttpContextAccessor();

		services.AddScoped<IEventBus, EventBus>();
		services.AddSingleton<IClock, SystemClock>();
		services.AddSingleton<IIdGenerator, SnowflakeIdGenerator>();
		services.AddSingleton<IInviteCodeGenerator, InviteCodeGenerator>();
		services.AddScoped<ICurrentUser, CurrentUser>();

		services.AddHttpClient<IUserService, UserServiceClient>((sp, client) =>
		{
			var opts = sp.GetRequiredService<IOptions<UserServiceOptions>>().Value;
			client.BaseAddress = new Uri(opts.BaseUrl);
			client.Timeout = TimeSpan.FromSeconds(2);
		});

		return services;
	}

	private static OptionsBuilder<T> ConfigureOptions<T>(IServiceCollection services)
		where T : class, IOptions
	{
		return services.AddOptions<T>()
			.BindConfiguration(T.SectionName)
			.ValidateDataAnnotations()
			.ValidateOnStart();
	}
}
