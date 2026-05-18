using Chat.Application.Contracts;
using Chat.FunctionalTests.Infrastructure;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Xunit;

namespace Chat.FunctionalTests.Endpoints;

public sealed class SendMessageHubTests(ChatApiFactory factory) : IClassFixture<ChatApiFactory>, IAsyncLifetime
{
	private const long SendMessagesPermission = 1L << 0;

	public Task InitializeAsync()
	{
		// reset shared fake state so tests within the class fixture are
		// independent. xUnit calls this once per test before construction
		factory.GuildClient.Result = null;
		factory.MessageRepository.Reset();
		factory.EventBus.Reset();
		factory.Broadcaster.Reset();
		return Task.CompletedTask;
	}

	public Task DisposeAsync() => Task.CompletedTask;

	/// <summary>
	/// builds a HubConnection wired into the in-process test server. the
	/// standard ASP.NET pattern is to override HttpMessageHandlerFactory so
	/// negotiation goes through TestServer, and to override WebSocketFactory
	/// so the WebSocket transport tunnels through TestServer's bidirectional
	/// stream. Kestrel never actually binds in test mode (good)
	///
	/// JWT is forwarded BOTH via the Authorization header (CreateWebSocketClient
	/// adds it to the upgrade request) AND via ?access_token=... in the query
	/// string (Program.cs's JwtBearer OnMessageReceived prefers query for hub
	/// paths). either one alone is sufficient
	/// </summary>
	private HubConnection BuildHubConnection(string? token)
	{
		var server = factory.Server;
		var hubUri = new UriBuilder(server.BaseAddress) { Path = "/hubs/chat" }.Uri;

		return new HubConnectionBuilder()
			.WithUrl(hubUri, options =>
			{
				options.Transports = HttpTransportType.WebSockets;
				options.SkipNegotiation = true;
				options.HttpMessageHandlerFactory = _ => server.CreateHandler();
				options.WebSocketFactory = async (context, ct) =>
				{
					var wsClient = server.CreateWebSocketClient();
					wsClient.ConfigureRequest = req =>
					{
						if (token is not null)
							req.Headers["Authorization"] = $"Bearer {token}";
					};

					var uri = context.Uri;
					if (token is not null)
					{
						var separator = string.IsNullOrEmpty(uri.Query) ? "?" : "&";
						uri = new Uri(uri + separator + "access_token=" + token);
					}

					return await wsClient.ConnectAsync(uri, ct);
				};
			})
			.Build();
	}

	[Fact]
	public async Task Connect_WithoutToken_IsRejected()
	{
		var connection = BuildHubConnection(token: null);

		// [Authorize] on the hub causes the upgrade to fail; SignalR surfaces
		// this as an exception bubbling out of StartAsync
		await Assert.ThrowsAnyAsync<Exception>(() => connection.StartAsync());
		Assert.NotEqual(HubConnectionState.Connected, connection.State);

		await connection.DisposeAsync();
	}

	[Fact]
	public async Task Connect_WithToken_SucceedsAndConnects()
	{
		var token = TestTokens.Issue(ChatApiFactory.JwtSecret, userId: 42);
		var connection = BuildHubConnection(token);

		await connection.StartAsync();

		Assert.Equal(HubConnectionState.Connected, connection.State);

		await connection.DisposeAsync();
	}

	[Fact]
	public async Task SendMessage_HappyPath_PersistsAndPublishesAndBroadcasts()
	{
		factory.WithMembership(channelId: 100, userId: 42, guildId: 9, permissions: SendMessagesPermission);

		var token = TestTokens.Issue(ChatApiFactory.JwtSecret, userId: 42);
		var connection = BuildHubConnection(token);

		// subscribe to Error so we can assert the hub did NOT push one to the
		// caller. ReceiveMessage isn't observed: users aren't auto-joined to
		// channel groups yet (#66), so the test asserts on the broadcaster
		// fake rather than on a round-trip through SignalR
		var errors = new List<(string Code, string Message)>();
		connection.On<string, string>("Error", (code, message) => errors.Add((code, message)));

		await connection.StartAsync();

		await connection.InvokeAsync("SendMessage", new
		{
			ChannelId = 100L,
			Content = "hello world",
			ReplyToId = (long?)null,
		});

		await connection.DisposeAsync();

		Assert.Empty(errors);

		var saved = Assert.Single(factory.SavedMessages);
		Assert.Equal(100L, saved.ChannelId);
		Assert.Equal(42L, saved.AuthorId);
		Assert.Equal("hello world", saved.Content);

		var evt = Assert.Single(factory.EventBus.PublishedOf<ChatMessageSent>());
		Assert.Equal(100L, evt.ChannelId);
		Assert.Equal(9L, evt.GuildId);
		Assert.Equal(42L, evt.AuthorId);
		Assert.Equal(saved.Id, evt.MessageId);

		var (broadcastChannel, broadcastResponse) = Assert.Single(factory.Broadcaster.Broadcasts);
		Assert.Equal(100L, broadcastChannel);
		Assert.Equal(saved.Id.ToString(), broadcastResponse.Id);
	}

	[Fact]
	public async Task SendMessage_MissingPermission_SurfacesErrorEventToCaller_NoSideEffects()
	{
		factory.WithMembership(channelId: 100, userId: 42, guildId: 9, permissions: 0);

		var token = TestTokens.Issue(ChatApiFactory.JwtSecret, userId: 42);
		var connection = BuildHubConnection(token);

		// completion source so the test can await the Error push instead of
		// guessing at a timeout. timeout is the safety net, not the primary
		// synchronisation
		var errorReceived = new TaskCompletionSource<(string Code, string Message)>(
			TaskCreationOptions.RunContinuationsAsynchronously);
		connection.On<string, string>("Error", (code, message) =>
			errorReceived.TrySetResult((code, message)));

		await connection.StartAsync();

		await connection.InvokeAsync("SendMessage", new
		{
			ChannelId = 100L,
			Content = "hi",
			ReplyToId = (long?)null,
		});

		var winner = await Task.WhenAny(errorReceived.Task, Task.Delay(TimeSpan.FromSeconds(2)));
		Assert.Same(errorReceived.Task, winner);
		var (code, _) = await errorReceived.Task;
		Assert.Equal("Message.MissingSendPermission", code);

		await connection.DisposeAsync();

		Assert.Empty(factory.SavedMessages);
		Assert.Empty(factory.EventBus.PublishedOf<ChatMessageSent>());
		Assert.Empty(factory.Broadcaster.Broadcasts);
	}
}
