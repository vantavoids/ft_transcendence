using Chat.FunctionalTests.Infrastructure;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Xunit;

namespace Chat.FunctionalTests.Endpoints;

/// <summary>
/// smoke tests for ChatHub connection/auth. sending messages is REST-only
/// per contract — see SendMessageEndpointTests for those cases.
/// </summary>
public sealed class ChatHubTests(ChatApiFactory factory) : IClassFixture<ChatApiFactory>
{
	private HubConnection BuildHubConnection(string? token)
	{
		var server = factory.Server;
		var hubUri = new UriBuilder(server.BaseAddress) { Path = "/v1/hubs/chat" }.Uri;

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
}
