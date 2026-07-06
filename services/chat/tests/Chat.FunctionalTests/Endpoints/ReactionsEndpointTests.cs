using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Chat.Application.Abstractions;
using Chat.Domain.Messages;
using Chat.FunctionalTests.Infrastructure;
using Xunit;

namespace Chat.FunctionalTests.Endpoints;

public sealed class ReactionsEndpointTests(ChatApiFactory factory)
	: IClassFixture<ChatApiFactory>, IAsyncLifetime
{
	private const long ReadMessagesPermission = 1L << 1;

	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
		PropertyNameCaseInsensitive = true,
	};

	public Task InitializeAsync()
	{
		factory.GuildClient.Result = null;
		factory.MessageRepository.Reset();
		factory.ReactionRepository.Reset();
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

	private static Message SeedChannelMessage(ChatApiFactory f, long id, long channelId, long authorId = 99, bool isDeleted = false)
	{
		var message = Message.Reconstitute(
			id: id, containerId: channelId, authorId: authorId, recipientId: null,
			content: "hello", replyToId: null, editedAt: null,
			isDeleted: isDeleted, createdAt: DateTimeOffset.UtcNow);
		f.MessageRepository.Seed(message);
		return message;
	}

	private static Message SeedDirectMessage(ChatApiFactory f, long id, long conversationId, long authorId, long recipientId)
	{
		var message = Message.Reconstitute(
			id: id, containerId: conversationId, authorId: authorId, recipientId: recipientId,
			content: "hello", replyToId: null, editedAt: null,
			isDeleted: false, createdAt: DateTimeOffset.UtcNow);
		f.MessageRepository.Seed(message);
		return message;
	}

	private static string ReactionUrl(long messageId, string emoji) =>
		$"/v1/messages/{messageId}/reactions/{Uri.EscapeDataString(emoji)}";

	[Fact]
	public async Task Put_WithoutToken_Returns401()
	{
		var response = await factory.CreateClient().PutAsync(ReactionUrl(1, "👍"), content: null);

		Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
	}

	[Fact]
	public async Task Put_HappyPath_Returns200AndBroadcasts()
	{
		SeedChannelMessage(factory, id: 1, channelId: 100);
		factory.GuildClient.Result = new ChannelMembership(IsMember: true, GuildId: 5, Permissions: ReadMessagesPermission);
		var client = BuildClient(userId: 42);

		var response = await client.PutAsync(ReactionUrl(1, "👍"), content: null);

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		var body = await response.Content.ReadFromJsonAsync<ReactionBody>(JsonOptions);
		Assert.NotNull(body);
		Assert.Equal("👍", body.Emoji);
		Assert.Equal(1, body.Count);
		Assert.True(body.MeReacted);

		var (channelId, evt) = Assert.Single(factory.Broadcaster.ReactionAddedBroadcasts);
		Assert.Equal(100L, channelId);
		Assert.Equal("1", evt.MessageId);
		Assert.Equal("👍", evt.Emoji);
		Assert.Equal(1, evt.Count);
	}

	[Fact]
	public async Task Put_MessageNotFound_Returns404()
	{
		var client = BuildClient(userId: 42);

		var response = await client.PutAsync(ReactionUrl(999, "👍"), content: null);

		Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
	}

	[Fact]
	public async Task Put_DirectMessage_Returns404()
	{
		SeedDirectMessage(factory, id: 1, conversationId: 555, authorId: 42, recipientId: 100);
		var client = BuildClient(userId: 42);

		var response = await client.PutAsync(ReactionUrl(1, "👍"), content: null);

		Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
		Assert.Empty(factory.Broadcaster.ReactionAddedBroadcasts);
	}

	[Fact]
	public async Task Put_NotAMember_Returns403()
	{
		SeedChannelMessage(factory, id: 1, channelId: 100);
		factory.GuildClient.Result = new ChannelMembership(IsMember: false, GuildId: 5, Permissions: 0);
		var client = BuildClient(userId: 42);

		var response = await client.PutAsync(ReactionUrl(1, "👍"), content: null);

		Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
	}

	[Fact]
	public async Task Put_MissingReadPermission_Returns403()
	{
		SeedChannelMessage(factory, id: 1, channelId: 100);
		factory.GuildClient.Result = new ChannelMembership(IsMember: true, GuildId: 5, Permissions: 0);
		var client = BuildClient(userId: 42);

		var response = await client.PutAsync(ReactionUrl(1, "👍"), content: null);

		Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
	}

	[Fact]
	public async Task Put_Repeated_IsIdempotent_ReturnsSameCount_SingleBroadcast()
	{
		SeedChannelMessage(factory, id: 1, channelId: 100);
		factory.GuildClient.Result = new ChannelMembership(IsMember: true, GuildId: 5, Permissions: ReadMessagesPermission);
		var client = BuildClient(userId: 42);

		var first = await client.PutAsync(ReactionUrl(1, "👍"), content: null);
		var second = await client.PutAsync(ReactionUrl(1, "👍"), content: null);

		Assert.Equal(HttpStatusCode.OK, first.StatusCode);
		Assert.Equal(HttpStatusCode.OK, second.StatusCode);

		var secondBody = await second.Content.ReadFromJsonAsync<ReactionBody>(JsonOptions);
		Assert.Equal(1, secondBody!.Count);
		Assert.Single(factory.Broadcaster.ReactionAddedBroadcasts);
	}

	[Fact]
	public async Task Delete_HappyPath_Returns200AndBroadcasts()
	{
		SeedChannelMessage(factory, id: 1, channelId: 100);
		factory.GuildClient.Result = new ChannelMembership(IsMember: true, GuildId: 5, Permissions: ReadMessagesPermission);
		factory.ReactionRepository.Seed(channelId: 100, messageId: 1, emoji: "👍", userId: 42);
		var client = BuildClient(userId: 42);

		var response = await client.DeleteAsync(ReactionUrl(1, "👍"));

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		var body = await response.Content.ReadFromJsonAsync<ReactionBody>(JsonOptions);
		Assert.NotNull(body);
		Assert.Equal("👍", body.Emoji);
		Assert.Equal(0, body.Count);
		Assert.False(body.MeReacted);

		var (channelId, evt) = Assert.Single(factory.Broadcaster.ReactionRemovedBroadcasts);
		Assert.Equal(100L, channelId);
		Assert.Equal("1", evt.MessageId);
		Assert.Equal("👍", evt.Emoji);
		Assert.Equal(0, evt.Count);
	}

	[Fact]
	public async Task Delete_NeverReacted_IsIdempotent_ReturnsCurrentCount_NoBroadcast()
	{
		SeedChannelMessage(factory, id: 1, channelId: 100);
		factory.GuildClient.Result = new ChannelMembership(IsMember: true, GuildId: 5, Permissions: ReadMessagesPermission);
		var client = BuildClient(userId: 42);

		var response = await client.DeleteAsync(ReactionUrl(1, "👍"));

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		var body = await response.Content.ReadFromJsonAsync<ReactionBody>(JsonOptions);
		Assert.Equal(0, body!.Count);
		Assert.Empty(factory.Broadcaster.ReactionRemovedBroadcasts);
	}

	[Fact]
	public async Task Delete_MessageNotFound_Returns404()
	{
		var client = BuildClient(userId: 42);

		var response = await client.DeleteAsync(ReactionUrl(999, "👍"));

		Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
	}

	[Fact]
	public async Task Delete_DirectMessage_Returns404()
	{
		SeedDirectMessage(factory, id: 1, conversationId: 555, authorId: 42, recipientId: 100);
		var client = BuildClient(userId: 42);

		var response = await client.DeleteAsync(ReactionUrl(1, "👍"));

		Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
		Assert.Empty(factory.Broadcaster.ReactionRemovedBroadcasts);
	}

	private sealed record ReactionBody(string Emoji, long Count, bool MeReacted);
}
