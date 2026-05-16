using System.Reflection;
using Guild.Application.Abstractions;
using Guild.Application.Abstractions.Security;
using Guild.Application.Contracts;
using Guild.Infrastructure.Messaging;
using Guild.Infrastructure.Options;
using Guild.Infrastructure.Security;
using MassTransit;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Guild.Infrastructure;

public static class DependencyInjection
{
	public static IServiceCollection AddInfrastructure(this IServiceCollection services)
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
			x.UsingRabbitMq((ctx, cfg) =>
			{
				var options = ctx.GetRequiredService<IOptions<RabbitMqOptions>>().Value;
				cfg.Host(options.Host, options.VirtualHost, h =>
				{
					h.Username(options.Username);
					h.Password(options.Password);
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
		services.AddScoped<ICurrentUser, CurrentUser>();

		return services;
	}

	private static OptionsBuilder<T> ConfigureOptions<T>(IServiceCollection services)
		where T : class, Options.IOptions
	{
		return services.AddOptions<T>()
			.BindConfiguration(T.SectionName)
			.ValidateDataAnnotations()
			.ValidateOnStart();
	}
}
