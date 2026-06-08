using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Chat.Application.Features.Messages.Common;
using Chat.Domain.Messages;
using Chat.FunctionalTests.Infrastructure;
using Xunit;

namespace Chat.FunctionalTests.Endpoints;

public sealed class GetChannelMessagesEndpointTests(ChatApiFactory factory)
	: IClassFixture<ChatApiFactory>, IAsyncLifetime
{
	private const long ReadMessagesPermission = 1L << 1;

	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
		PropertyNameCaseInsensitive = true,
	};

	// all seeded messages use a date well before FakeClock's 2026-01-01 anchor
	private static readonly DateTimeOffset PastDate =
		new(2025, 6, 1, 12, 0, 0, TimeSpan.Zero);

	public Task InitializeAsync()
	{
		factory.GuildClient.Result = null;
		factory.MessageRepository.Reset();
		factory.Broadcaster.Reset();
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

	private static void SeedMessage(ChatApiFactory f, long id, long channelId, DateTimeOffset? createdAt = null, bool isDeleted = false)
	{
		var message = Message.Reconstitute(
			id: id, channelId: channelId, authorId: 99,
			content: "hello", replyToId: null, editedAt: null,
			isDeleted: isDeleted, createdAt: createdAt ?? PastDate);
		f.MessageRepository.Seed(message);
	}

	[Fact]
	public async Task Get_WithoutToken_Returns401()
	{
		var response = await factory.CreateClient().GetAsync("/v1/channels/100/messages");

		Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
	}

	[Fact]
	public async Task Get_ChannelNotFound_Returns404()
	{
		factory.GuildClient.Result = null;
		var client = BuildClient(userId: 42);

		var response = await client.GetAsync("/v1/channels/100/messages");

		Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
	}

	[Fact]
	public async Task Get_NotAMember_Returns403()
	{
		factory.GuildClient.Result = new Chat.Application.Abstractions.ChannelMembership(
			IsMember: false, GuildId: 5, Permissions: ReadMessagesPermission);
		var client = BuildClient(userId: 42);

		var response = await client.GetAsync("/v1/channels/100/messages");

		Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
	}

	[Fact]
	public async Task Get_MissingReadPermission_Returns403()
	{
		factory.GuildClient.Result = new Chat.Application.Abstractions.ChannelMembership(
			IsMember: true, GuildId: 5, Permissions: 0);
		var client = BuildClient(userId: 42);

		var response = await client.GetAsync("/v1/channels/100/messages");

		Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
	}

	[Fact]
	public async Task Get_HappyPath_Returns200WithMessages()
	{
		factory.GuildClient.Result = new Chat.Application.Abstractions.ChannelMembership(
			IsMember: true, GuildId: 5, Permissions: ReadMessagesPermission);
		SeedMessage(factory, id: 1, channelId: 100, createdAt: PastDate.AddMinutes(-10));
		SeedMessage(factory, id: 2, channelId: 100, createdAt: PastDate.AddMinutes(-5));
		SeedMessage(factory, id: 3, channelId: 100, createdAt: PastDate.AddMinutes(-1), isDeleted: true);
		var client = BuildClient(userId: 42);

		var response = await client.GetAsync("/v1/channels/100/messages");

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		var messages = await response.Content.ReadFromJsonAsync<List<MessageResponse>>(JsonOptions);
		Assert.NotNull(messages);
		Assert.Equal(2, messages.Count);
		Assert.DoesNotContain(messages, m => m.Id == "3");
	}

	[Fact]
	public async Task Get_WithBeforeTime_ReturnsOnlyOlderMessages()
	{
		factory.GuildClient.Result = new Chat.Application.Abstractions.ChannelMembership(
			IsMember: true, GuildId: 5, Permissions: ReadMessagesPermission);
		var cursor = PastDate;
		SeedMessage(factory, id: 1, channelId: 100, createdAt: cursor.AddMinutes(-20));
		SeedMessage(factory, id: 2, channelId: 100, createdAt: cursor.AddMinutes(-10));
		SeedMessage(factory, id: 3, channelId: 100, createdAt: cursor.AddMinutes(5));
		var client = BuildClient(userId: 42);

		var beforeTimeStr = Uri.EscapeDataString(cursor.ToString("O"));
		var response = await client.GetAsync($"/v1/channels/100/messages?before_time={beforeTimeStr}");

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		var messages = await response.Content.ReadFromJsonAsync<List<MessageResponse>>(JsonOptions);
		Assert.NotNull(messages);
		Assert.Equal(2, messages.Count);
		Assert.DoesNotContain(messages, m => m.Id == "3");
	}
}
