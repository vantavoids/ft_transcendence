using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Chat.Application.Features.Channels.Common;
using Chat.FunctionalTests.Infrastructure;
using Xunit;

namespace Chat.FunctionalTests.Endpoints;

public sealed class GetChannelReadStatesEndpointTests(ChatApiFactory factory)
	: IClassFixture<ChatApiFactory>, IAsyncLifetime
{
	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
		PropertyNameCaseInsensitive = true,
	};

	public Task InitializeAsync()
	{
		factory.GuildClient.VisibleChannelIds = [];
		factory.ReadStateRepository.Reset();
		return Task.CompletedTask;
	}

	public Task DisposeAsync() => Task.CompletedTask;

	private HttpClient BuildClient(long userId)
	{
		var client = factory.CreateClient();
		var token = TestTokens.Issue(ChatApiFactory.JwtSecret, userId: userId);
		client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
		return client;
	}

	[Fact]
	public async Task Get_WithoutToken_Returns401()
	{
		var response = await factory.CreateClient().GetAsync("/v1/channels/read-states");

		Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
	}

	[Fact]
	public async Task Get_NoVisibleChannels_ReturnsEmptyArray()
	{
		var client = BuildClient(userId: 42);

		var response = await client.GetAsync("/v1/channels/read-states");

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		var body = await response.Content.ReadFromJsonAsync<List<ChannelReadStateResponse>>(JsonOptions);
		Assert.Empty(body!);
	}

	[Fact]
	public async Task Get_ChannelNeverRead_ReturnsNullCursor_AndTotalCount()
	{
		factory.GuildClient.VisibleChannelIds = [100];
		factory.ReadStateRepository.ChannelMessageCountsAfter[100] = 4;
		var client = BuildClient(userId: 42);

		var response = await client.GetAsync("/v1/channels/read-states");

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		var body = await response.Content.ReadFromJsonAsync<List<ChannelReadStateResponse>>(JsonOptions);
		var entry = Assert.Single(body!);
		Assert.Equal("100", entry.ChannelId);
		Assert.Null(entry.LastReadMessageId);
		Assert.Equal(4, entry.UnreadCount);
	}
}
