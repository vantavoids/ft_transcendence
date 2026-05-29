using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Chat.Application.Contracts;
using Chat.FunctionalTests.Infrastructure;
using Xunit;

namespace Chat.FunctionalTests.Endpoints;

public sealed class SendMessageEndpointTests(ChatApiFactory factory)
	: IClassFixture<ChatApiFactory>, IAsyncLifetime
{
	private const long SendMessagesPermission = 1L << 0;

	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
		PropertyNameCaseInsensitive = true,
	};

	public Task InitializeAsync()
	{
		factory.GuildClient.Result = null;
		factory.MessageRepository.Reset();
		factory.EventBus.Reset();
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

	[Fact]
	public async Task Post_WithoutToken_Returns401()
	{
		var client = factory.CreateClient();

		var response = await client.PostAsJsonAsync("/v1/channels/100/messages",
			new { content = "hi" });

		Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
	}

	[Fact]
	public async Task Post_HappyPath_Returns201_PersistsAndPublishesAndBroadcasts()
	{
		factory.WithMembership(channelId: 100, userId: 42, guildId: 9, permissions: SendMessagesPermission);
		var client = BuildClient(userId: 42);

		var response = await client.PostAsJsonAsync("/v1/channels/100/messages",
			new { content = "hello world", nonce = "test-nonce-1" });

		Assert.Equal(HttpStatusCode.Created, response.StatusCode);

		var body = await response.Content.ReadFromJsonAsync<MessageBody>(JsonOptions);
		Assert.NotNull(body);
		Assert.Equal("100", body.ChannelId);
		Assert.Equal("42", body.AuthorId);
		Assert.Equal("hello world", body.Content);
		Assert.Null(body.ReplyToId);
		Assert.Null(body.EditedAt);
		Assert.Equal("test-nonce-1", body.Nonce);
		Assert.True(long.TryParse(body.Id, out var messageId));

		var saved = Assert.Single(factory.SavedMessages);
		Assert.Equal(messageId, saved.Id);
		Assert.Equal(100L, saved.ChannelId);
		Assert.Equal(42L, saved.AuthorId);

		var evt = Assert.Single(factory.EventBus.PublishedOf<ChatMessageSent>());
		Assert.Equal(100L, evt.ChannelId);
		Assert.Equal(9L, evt.GuildId);
		Assert.Equal(42L, evt.AuthorId);
		Assert.Equal(messageId, evt.MessageId);

		var (broadcastChannel, broadcastMsg) = Assert.Single(factory.Broadcaster.Broadcasts);
		Assert.Equal(100L, broadcastChannel);
		Assert.Equal(body.Id, broadcastMsg.Id);
		Assert.Equal("test-nonce-1", broadcastMsg.Nonce);
	}

	[Fact]
	public async Task Post_MissingPermission_Returns403_NoSideEffects()
	{
		factory.WithMembership(channelId: 100, userId: 42, guildId: 9, permissions: 0);
		var client = BuildClient(userId: 42);

		var response = await client.PostAsJsonAsync("/v1/channels/100/messages",
			new { content = "hi" });

		Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
		Assert.Empty(factory.SavedMessages);
		Assert.Empty(factory.EventBus.Published);
		Assert.Empty(factory.Broadcaster.Broadcasts);
	}

	[Fact]
	public async Task Post_ChannelNotFound_Returns404()
	{
		factory.GuildClient.Result = null;
		var client = BuildClient(userId: 42);

		var response = await client.PostAsJsonAsync("/v1/channels/100/messages",
			new { content = "hi" });

		Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
	}

	[Fact]
	public async Task Post_NonceTooLong_Returns400()
	{
		factory.WithMembership(channelId: 100, userId: 42, guildId: 9, permissions: SendMessagesPermission);
		var client = BuildClient(userId: 42);
		var longNonce = new string('x', 65);

		var response = await client.PostAsJsonAsync("/v1/channels/100/messages",
			new { content = "hi", nonce = longNonce });

		Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
	}

	[Fact]
	public async Task Post_NoNonce_Returns201WithNullNonce()
	{
		factory.WithMembership(channelId: 100, userId: 42, guildId: 9, permissions: SendMessagesPermission);
		var client = BuildClient(userId: 42);

		var response = await client.PostAsJsonAsync("/v1/channels/100/messages",
			new { content = "hello" });

		Assert.Equal(HttpStatusCode.Created, response.StatusCode);
		var body = await response.Content.ReadFromJsonAsync<MessageBody>(JsonOptions);
		Assert.Null(body!.Nonce);
	}

	[Fact]
	public async Task Post_SameNonce_Returns201WithOriginalMessage()
	{
		factory.WithMembership(channelId: 100, userId: 42, guildId: 9, permissions: SendMessagesPermission);
		var client = BuildClient(userId: 42);

		var first = await client.PostAsJsonAsync("/v1/channels/100/messages",
			new { content = "hello", nonce = "dedup-nonce" });
		var firstBody = await first.Content.ReadFromJsonAsync<MessageBody>(JsonOptions);

		var second = await client.PostAsJsonAsync("/v1/channels/100/messages",
			new { content = "hello", nonce = "dedup-nonce" });
		var secondBody = await second.Content.ReadFromJsonAsync<MessageBody>(JsonOptions);

		Assert.Equal(HttpStatusCode.Created, second.StatusCode);
		Assert.Equal(firstBody!.Id, secondBody!.Id);
		Assert.Equal(firstBody.CreatedAt, secondBody.CreatedAt);
		Assert.Equal("dedup-nonce", secondBody.Nonce);
		Assert.Single(factory.SavedMessages);
	}

	private sealed record MessageBody(
		string Id,
		string ChannelId,
		string AuthorId,
		string? Content,
		string? ReplyToId,
		DateTimeOffset? EditedAt,
		DateTimeOffset CreatedAt,
		string? Nonce);
}
