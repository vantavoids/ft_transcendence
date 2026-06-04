using System.Text.Json;
using Chat.Application.Abstractions;
using Chat.Application.Abstractions.Authentication;
using Chat.Application.Contracts;
using Chat.Infrastructure.Authentication;
using Chat.Infrastructure.Http;
using Chat.Infrastructure.Messaging;
using Chat.Infrastructure.Messaging.Consumers;
using Chat.Infrastructure.Messaging.Contracts;
using Chat.Infrastructure.Options;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Chat.Infrastructure;

public static class DependencyInjection
{
	public static IServiceCollection AddInfrastructure(this IServiceCollection services)
	{
		services.AddOptions<BackendConfigurationOptions>()
			.BindConfiguration("BackendConfiguration")
			.ValidateDataAnnotations()
			.ValidateOnStart();

		services.AddOptions<ServicesOptions>()
			.BindConfiguration("Services")
			.ValidateDataAnnotations()
			.ValidateOnStart();

		services.AddOptions<RabbitMqOptions>()
			.BindConfiguration("RabbitMQ")
			.ValidateDataAnnotations()
			.ValidateOnStart();

		services.AddOptions<SnowflakeOptions>()
			.BindConfiguration("Snowflake")
			.ValidateDataAnnotations()
			.ValidateOnStart();

		services.AddMassTransit(x =>
		{
			x.AddConsumer<GuildMemberJoinedConsumer>();
			x.AddConsumer<GuildMemberLeftConsumer>();
			x.AddConsumer<UserLoggedOutConsumer>();

			x.UsingRabbitMq((ctx, cfg) =>
			{
				var options = ctx.GetRequiredService<IOptions<RabbitMqOptions>>().Value;
				cfg.Host(options.Host, options.VirtualHost, h =>
				{
					h.Username(options.Username);
					h.Password(options.Password);
				});

				// docs/contracts are the source of truth and document snake_case
				// payloads. MassTransit defaults to camelCase via System.Text.Json,
				// so override the property naming policy to match
				cfg.ConfigureJsonSerializerOptions(opts =>
				{
					opts.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
					return opts;
				});

				cfg.Message<ChatMessageSent>(m => m.SetEntityName("chat.message_sent"));
				cfg.Message<CallIncoming>(m => m.SetEntityName("call.incoming"));
				cfg.Message<UserOnline>(m => m.SetEntityName("user.online"));
				cfg.Message<UserOffline>(m => m.SetEntityName("user.offline"));
				cfg.Message<GuildMemberJoined>(m => m.SetEntityName("guild.member_joined"));
				cfg.Message<GuildMemberLeft>(m => m.SetEntityName("guild.member_left"));
				cfg.Message<UserLoggedOut>(m => m.SetEntityName("user.logged_out"));

				cfg.ConfigureEndpoints(ctx);
			});
		});

		services.AddHttpContextAccessor();

		services.AddScoped<IEventBus, EventBus>();
		services.AddSingleton<IClock, SystemClock>();
		services.AddSingleton<ISnowflakeIdGenerator, SnowflakeIdGenerator>();
		services.AddScoped<ICurrentUser, CurrentUser>();

		services.AddHttpClient<IGuildClient, GuildClient>((sp, c) =>
		{
			var opts = sp.GetRequiredService<IOptions<ServicesOptions>>().Value;
			c.BaseAddress = new Uri(opts.GuildService.TrimEnd('/') + "/internal/");
		});

		return services;
	}
}
