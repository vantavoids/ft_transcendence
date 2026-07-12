using System.Reflection;
using System.Text.Json;
using Guild.Application;
using Guild.Application.Abstractions;
using Guild.Application.Abstractions.Security;
using Guild.Application.Abstractions.Users;
using Guild.Application.Contracts;
using Guild.Infrastructure.Messaging;
using Guild.Infrastructure.Messaging.Consumers;
using Guild.Infrastructure.Messaging.Contracts;
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

			// consumes Auth's user.deleted to run the GDPR erasure cascade (#147)
			x.AddConsumer<UserDeletedConsumer>();

			x.UsingRabbitMq((ctx, cfg) =>
			{
				var options = ctx.GetRequiredService<IOptions<RabbitMqOptions>>().Value;
				cfg.Host(options.Host, options.VirtualHost, h =>
				{
					h.Username(options.Username);
					h.Password(options.Password);
				});

				// the docs/contracts source of truth specify snake_case payloads with
				// snowflake ids as quoted strings. MassTransit defaults to camelCase
				// via System.Text.Json, so override the wire format to match the docs.
				// every other service that publishes or consumes these events must
				// apply the same policy or deserialisation will fail
				cfg.ConfigureJsonSerializerOptions(GuildSerialization.ApplyEventWireFormat);

				// publish raw JSON bodies (no MassTransit envelope) so polyglot consumers
				// (Go/NestJS) read the contract payload directly; the message type travels
				// in transport headers so .NET consumers still bind.
				cfg.UseRawJsonSerializer(
					RawSerializerOptions.AddTransportHeaders | RawSerializerOptions.CopyHeaders);

				cfg.Message<GuildMemberJoined>(m => m.SetEntityName("guild.member_joined"));
				cfg.Message<GuildMemberLeft>(m => m.SetEntityName("guild.member_left"));
				cfg.Message<GuildInviteCreated>(m => m.SetEntityName("guild.invite_created"));
				cfg.Message<GuildDeleted>(m => m.SetEntityName("guild.deleted"));
				cfg.Message<GuildOwnerTransferred>(m => m.SetEntityName("guild.owner_transferred"));
				cfg.Message<GuildUpdated>(m => m.SetEntityName("guild.updated"));
				cfg.Message<GuildRolesChanged>(m => m.SetEntityName("guild.roles_changed"));
				cfg.Message<GuildMemberUpdated>(m => m.SetEntityName("guild.member_updated"));
				cfg.Message<ChannelAccessRevoked>(m => m.SetEntityName("channel.access_revoked"));
				cfg.Message<ChannelAccessGranted>(m => m.SetEntityName("channel.access_granted"));
				cfg.Message<GuildChannelCreated>(m => m.SetEntityName("channel.created"));
				cfg.Message<GuildChannelUpdated>(m => m.SetEntityName("channel.updated"));
				cfg.Message<GuildChannelDeleted>(m => m.SetEntityName("channel.deleted"));
				cfg.Message<GuildCategoryCreated>(m => m.SetEntityName("category.created"));
				cfg.Message<GuildCategoryUpdated>(m => m.SetEntityName("category.updated"));
				cfg.Message<GuildCategoryDeleted>(m => m.SetEntityName("category.deleted"));

				// inbound: bind the consumer's receive endpoint to Auth's user.deleted
				// exchange. must match the SetEntityName Auth publishes under.
				cfg.Message<UserDeleted>(m => m.SetEntityName("user.deleted"));

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
