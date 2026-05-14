using Guild.Application.Abstractions;
using Guild.Application.Contracts;
using Guild.Infrastructure.Messaging;
using Guild.Infrastructure.Options;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Guild.Infrastructure;

public static class DependencyInjection
{
	public static IServiceCollection AddInfrastructure(this IServiceCollection services)
	{
		services.AddOptions<BackendConfigurationOptions>()
			.BindConfiguration("BackendConfiguration")
			.ValidateDataAnnotations()
			.ValidateOnStart();

		services.AddOptions<RabbitMqOptions>()
			.BindConfiguration("RabbitMQ")
			.ValidateDataAnnotations()
			.ValidateOnStart();

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

		services.AddScoped<IEventBus, EventBus>();

		return services;
	}
}
