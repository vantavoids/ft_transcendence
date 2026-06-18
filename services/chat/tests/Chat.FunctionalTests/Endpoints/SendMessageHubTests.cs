using Chat.FunctionalTests.Infrastructure;
using Microsoft.AspNetCore.SignalR.Client;
using Xunit;

namespace Chat.FunctionalTests.Endpoints;

/// <summary>
/// smoke tests for ChatHub connection/auth. sending messages is REST-only
/// per contract — see SendMessageEndpointTests for those cases.
/// </summary>
public sealed class ChatHubTests(ChatApiFactory factory) : IClassFixture<ChatApiFactory>
{
	[Fact]
	public async Task Connect_WithoutToken_IsRejected()
	{
		var connection = HubConnectionHelper.Build(factory, token: null);

		await Assert.ThrowsAnyAsync<Exception>(() => connection.StartAsync());
		Assert.NotEqual(HubConnectionState.Connected, connection.State);

		await connection.DisposeAsync();
	}

	[Fact]
	public async Task Connect_WithToken_SucceedsAndConnects()
	{
		var token = TestTokens.Issue(ChatApiFactory.JwtSecret, userId: 42);
		await using var connection = HubConnectionHelper.Build(factory, token);

		await connection.StartAsync();

		Assert.Equal(HubConnectionState.Connected, connection.State);
	}
}
