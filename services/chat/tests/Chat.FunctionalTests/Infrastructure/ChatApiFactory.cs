using Cassandra;
using Chat.Application.Abstractions;
using Chat.Application.Abstractions.Persistence;
using Chat.Application.Features.Messages.Common;
using Chat.Presentation.Hubs;
using Chat.Domain.Messages;
using Chat.UnitTests.Fakes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Chat.FunctionalTests.Infrastructure;

/// <summary>
/// boots the Chat Service against in-memory fakes (no Scylla, no RabbitMQ, no
/// real Guild Service). each factory instance gets its own set of fakes so test
/// classes don't bleed state into each other
/// </summary>
public sealed class ChatApiFactory : WebApplicationFactory<Program>
{
	public const string JwtSecret =
		"0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";

	// Program.cs reads builder.Configuration["Jwt:SecretKey"] *eagerly* during
	// host startup, so set the env var here once for the whole process. all
	// factory instances write the same constant so this is safe
	static ChatApiFactory()
	{
		Environment.SetEnvironmentVariable("Jwt__SecretKey", JwtSecret);
	}

	// fakes are shared across the factory instance so tests can both inject
	// canned membership/permission rows and assert on what the handler did
	public FakeGuildClient GuildClient { get; } = new();
	public FakeUserClient UserClient { get; } = new();
	public FakeMessageRepository MessageRepository { get; } = new();
	public FakeEventBus EventBus { get; } = new();
	public FakeChannelBroadcaster Broadcaster { get; } = new();
	public FakeClock Clock { get; } = new();
	public FakeIdGenerator IdGenerator { get; } = new();

	public IReadOnlyList<Message> SavedMessages => MessageRepository.Saved;

	public void WithMembership(long channelId, long userId, long guildId, long permissions)
	{
		// FakeGuildClient ignores the lookup keys for now (it returns whatever
		// Result is set to). this signature lets the test stay declarative
		// even though the fake only models a single canned response
		_ = channelId;
		_ = userId;
		GuildClient.Result = new ChannelMembership(IsMember: true, GuildId: guildId, Permissions: permissions);
	}

	public void WithMembershipFor(long channelId, long userId, long guildId, long permissions)
		=> GuildClient.Setup(channelId, userId, new ChannelMembership(IsMember: true, GuildId: guildId, Permissions: permissions));

	public void WithoutMembershipFor(long channelId, long userId)
		=> GuildClient.Setup(channelId, userId, null);

	public void WithoutMembership() => GuildClient.Result = null;

	// Bypasses FakeChannelBroadcaster (which is a no-op for SignalR) and pushes
	// events directly into the hub group via IHubContext. Use to verify that a
	// subscriber in the group receives the correct hub invocation.
	public async Task SimulateMessageEditedAsync(long channelId, MessageEditedEvent evt)
	{
		var hub = Server.Services.GetRequiredService<IHubContext<ChatHub, IChatClient>>();
		await hub.Clients.Group($"channel:{channelId}").MessageEdited(evt);
	}

	public async Task SimulateMessageDeletedAsync(long channelId, long messageId)
	{
		var hub = Server.Services.GetRequiredService<IHubContext<ChatHub, IChatClient>>();
		await hub.Clients.Group($"channel:{channelId}").MessageDeleted(
			new MessageDeletedEvent(messageId.ToString(), channelId.ToString()));
	}

	// Directly calls the real SignalRUserBroadcaster (not a fake) so that
	// connected hub clients receive GuildJoined/GuildLeft invocations.
	// MassTransit is stripped in tests, so consumers cannot be triggered via
	// RabbitMQ — this method simulates the consumer side effect.
	public async Task SimulateGuildJoinedAsync(long userId, long guildId, string guildName)
	{
		using var scope = Server.Services.CreateScope();
		var broadcaster = scope.ServiceProvider.GetRequiredService<IUserBroadcaster>();
		await broadcaster.BroadcastGuildJoinedAsync(userId, guildId, guildName, CancellationToken.None);
	}

	public async Task SimulateGuildLeftAsync(long userId, long guildId)
	{
		using var scope = Server.Services.CreateScope();
		var broadcaster = scope.ServiceProvider.GetRequiredService<IUserBroadcaster>();
		await broadcaster.BroadcastGuildLeftAsync(userId, guildId, CancellationToken.None);
	}

	// server-side eviction primitive (the guild.member_left side effect): purges
	// the user's connections from every channel group they joined under the guild.
	public async Task SimulateGuildEvictionAsync(long userId, long guildId)
	{
		using var scope = Server.Services.CreateScope();
		var broadcaster = scope.ServiceProvider.GetRequiredService<IUserBroadcaster>();
		await broadcaster.EvictFromGuildChannelsAsync(userId, guildId, CancellationToken.None);
	}

	// server-side eviction primitive scoped to a single channel (the future
	// channel.access_revoked side effect): purges the user from one channel group.
	public async Task SimulateChannelEvictionAsync(long userId, long channelId)
	{
		using var scope = Server.Services.CreateScope();
		var broadcaster = scope.ServiceProvider.GetRequiredService<IUserBroadcaster>();
		await broadcaster.EvictFromChannelAsync(userId, channelId, CancellationToken.None);
	}

	// Bypasses IChannelBroadcaster (replaced by a fake) and pushes a message
	// directly into the SignalR channel group. Use to verify that a connected
	// client receives ReceiveMessage after JoinChannel.
	public async Task SimulateChannelMessageAsync(long channelId, MessageResponse message)
	{
		var hub = Server.Services.GetRequiredService<IHubContext<ChatHub, IChatClient>>();
		await hub.Clients.Group($"channel:{channelId}").ReceiveMessage(message);
	}

	protected override void ConfigureWebHost(IWebHostBuilder builder)
	{
		builder.UseEnvironment("Testing");

		builder.ConfigureAppConfiguration((_, config) =>
		{
			config.AddInMemoryCollection(new Dictionary<string, string?>
			{
				// ScyllaDbOptions still validates even though ISession is
				// replaced below; keep the bind happy
				["ScyllaDb:ContactPoints"] = "localhost",
				["ScyllaDb:Keyspace"] = "chat",
				["ScyllaDb:Port"] = "9042",

				["Jwt:SecretKey"] = JwtSecret,

				["RabbitMQ:Host"] = "localhost",
				["RabbitMQ:VirtualHost"] = "/",
				["RabbitMQ:Username"] = "test",
				["RabbitMQ:Password"] = "test",

				["Snowflake:WorkerId"] = "3",

				["BackendConfiguration:BaseUrl"] = "http://localhost",
				["BackendConfiguration:BaseApiUrl"] = "http://localhost/api",

				["Services:GuildService"] = "http://localhost:5101",
				["Services:UserService"] = "http://localhost:5101",
			});
		});

		builder.ConfigureTestServices(services =>
		{
			StripMassTransit(services);
			StripCassandra(services);

			// swap real ports for fakes; explicit singleton registration so
			// the fields on the factory and the values DI hands to handlers
			// reference the same instance
			services.RemoveAll<IEventBus>();
			services.AddSingleton<IEventBus>(EventBus);

			services.RemoveAll<IGuildClient>();
			services.AddSingleton<IGuildClient>(GuildClient);

			services.RemoveAll<IUserClient>();
			services.AddSingleton<IUserClient>(UserClient);

			services.RemoveAll<IMessageRepository>();
			services.AddSingleton<IMessageRepository>(MessageRepository);

			services.RemoveAll<IChannelBroadcaster>();
			services.AddSingleton<IChannelBroadcaster>(Broadcaster);

			services.RemoveAll<IClock>();
			services.AddSingleton<IClock>(Clock);

			services.RemoveAll<ISnowflakeIdGenerator>();
			services.AddSingleton<ISnowflakeIdGenerator>(IdGenerator);
		});
	}

	private static void StripMassTransit(IServiceCollection services)
	{
		var toRemove = services
			.Where(d =>
				(d.ServiceType.FullName ?? string.Empty)
					.StartsWith("MassTransit", StringComparison.Ordinal)
				|| (d.ImplementationType?.FullName ?? string.Empty)
					.StartsWith("MassTransit", StringComparison.Ordinal))
			.ToList();

		foreach (var d in toRemove)
			services.Remove(d);
	}

	private static void StripCassandra(IServiceCollection services)
	{
		// remove the singleton ISession factory so we never try to dial Scylla
		// from the test host. the IMessageRepository swap above means nothing
		// will resolve ISession from the container, but be explicit anyway
		services.RemoveAll<ISession>();
		services.RemoveAll<ICluster>();
	}
}
