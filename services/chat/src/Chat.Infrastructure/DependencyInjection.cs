using System.Text.Json;
using Amazon.S3;
using Chat.Application.Abstractions;
using Chat.Application.Abstractions.Authentication;
using Chat.Application.Contracts;
using Chat.Infrastructure.Authentication;
using Chat.Infrastructure.Http;
using Chat.Infrastructure.Messaging;
using Chat.Infrastructure.Messaging.Consumers;
using Chat.Infrastructure.Messaging.Contracts;
using Chat.Infrastructure.Options;
using Chat.Infrastructure.Storage;
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

		services.AddOptions<MinioOptions>()
			.BindConfiguration("Minio")
			.ValidateDataAnnotations()
			.ValidateOnStart();

		services.AddOptions<AttachmentReaperOptions>()
			.BindConfiguration("AttachmentReaper")
			.ValidateDataAnnotations()
			.ValidateOnStart();

		services.AddMassTransit(x =>
		{
			x.AddConsumer<GuildMemberJoinedConsumer>();
			x.AddConsumer<GuildMemberLeftConsumer>();
			x.AddConsumer<GuildDeletedConsumer>();
			x.AddConsumer<GuildUpdatedConsumer>();
			x.AddConsumer<ChannelAccessRevokedConsumer>();
			x.AddConsumer<GuildChannelCreatedConsumer>();
			x.AddConsumer<GuildChannelUpdatedConsumer>();
			x.AddConsumer<GuildChannelDeletedConsumer>();
			x.AddConsumer<GuildCategoryCreatedConsumer>();
			x.AddConsumer<GuildCategoryUpdatedConsumer>();
			x.AddConsumer<GuildCategoryDeletedConsumer>();
			x.AddConsumer<UserLoggedOutConsumer>();
			x.AddConsumer<UserDeletedConsumer>();

			x.UsingRabbitMq((ctx, cfg) =>
			{
				var options = ctx.GetRequiredService<IOptions<RabbitMqOptions>>().Value;
				cfg.Host(options.Host, options.VirtualHost, h =>
				{
					h.Username(options.Username);
					h.Password(options.Password);
				});

				// docs/contracts are the source of truth and document snake_case
				// payloads with snowflake ids as quoted strings. MassTransit defaults
				// to camelCase via System.Text.Json, so override the wire format to match
				cfg.ConfigureJsonSerializerOptions(opts =>
				{
					opts.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
					opts.Converters.Add(new SnowflakeJsonConverter());
					return opts;
				});

				// publish raw JSON bodies (no MassTransit envelope) so polyglot consumers
				// (Go/NestJS) read the contract payload directly; the message type travels
				// in transport headers so .NET consumers still bind.
				cfg.UseRawJsonSerializer(
					RawSerializerOptions.AddTransportHeaders | RawSerializerOptions.CopyHeaders);

				cfg.Message<ChatMessageSent>(m => m.SetEntityName("chat.message_sent"));
				cfg.Message<ChatDmSent>(m => m.SetEntityName("chat.dm_sent"));
				cfg.Message<CallIncoming>(m => m.SetEntityName("call.incoming"));
				cfg.Message<UserOnline>(m => m.SetEntityName("user.online"));
				cfg.Message<UserOffline>(m => m.SetEntityName("user.offline"));
				cfg.Message<GuildMemberJoined>(m => m.SetEntityName("guild.member_joined"));
				cfg.Message<GuildMemberLeft>(m => m.SetEntityName("guild.member_left"));
				cfg.Message<GuildDeleted>(m => m.SetEntityName("guild.deleted"));
				cfg.Message<GuildUpdated>(m => m.SetEntityName("guild.updated"));
				cfg.Message<ChannelAccessRevoked>(m => m.SetEntityName("channel.access_revoked"));
				cfg.Message<GuildChannelCreated>(m => m.SetEntityName("channel.created"));
				cfg.Message<GuildChannelUpdated>(m => m.SetEntityName("channel.updated"));
				cfg.Message<GuildChannelDeleted>(m => m.SetEntityName("channel.deleted"));
				cfg.Message<GuildCategoryCreated>(m => m.SetEntityName("category.created"));
				cfg.Message<GuildCategoryUpdated>(m => m.SetEntityName("category.updated"));
				cfg.Message<GuildCategoryDeleted>(m => m.SetEntityName("category.deleted"));
				cfg.Message<UserLoggedOut>(m => m.SetEntityName("user.logged_out"));

				cfg.ConfigureEndpoints(ctx);
			});
		});

		services.AddMemoryCache();
		services.AddHttpContextAccessor();

		services.AddScoped<IEventBus, EventBus>();
		services.AddSingleton<IClock, SystemClock>();
		services.AddSingleton<ISnowflakeIdGenerator, SnowflakeIdGenerator>();
		services.AddScoped<ICurrentUser, CurrentUser>();

		services.AddSingleton<IAmazonS3>(sp =>
		{
			var opts = sp.GetRequiredService<IOptions<MinioOptions>>().Value;
			var config = new AmazonS3Config
			{
				ServiceURL = opts.Endpoint,
				// MinIO only speaks path-style addressing (no virtual-host buckets)
				ForcePathStyle = true,
			};
			return new AmazonS3Client(opts.AccessKey, opts.SecretKey, config);
		});
		services.AddSingleton<IObjectStore, MinioObjectStore>();
		services.AddSingleton<IAttachmentUrlFactory, AttachmentUrlFactory>();
		services.AddHostedService<AttachmentReaper>();

		RegisterAndConfHttpClient<IGuildClient, GuildClient>(services, opts => opts.GuildService);
		RegisterAndConfHttpClient<IUserClient, UserClient>(services, opts => opts.UserService);
		return services;
	}

	private static void RegisterAndConfHttpClient<TInterface, T>(
		IServiceCollection services,
		Func<ServicesOptions, string> selectBaseUrl)
		where TInterface: class
		where T: class, TInterface
	{
		services.AddHttpClient<TInterface, T>((sp, c) =>
		{
			var opts = sp.GetRequiredService<IOptions<ServicesOptions>>().Value;
			c.BaseAddress = new Uri(selectBaseUrl(opts).TrimEnd('/') + "/internal/");
		});
	}
}
