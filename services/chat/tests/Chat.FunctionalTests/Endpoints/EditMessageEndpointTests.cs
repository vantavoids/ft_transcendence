using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Chat.Domain.Messages;
using Chat.FunctionalTests.Infrastructure;
using Xunit;

namespace Chat.FunctionalTests.Endpoints;

public sealed class EditMessageEndpointTests(ChatApiFactory factory)
	: IClassFixture<ChatApiFactory>, IAsyncLifetime
{
	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
		PropertyNameCaseInsensitive = true,
	};

	public Task InitializeAsync()
	{
		factory.GuildClient.Result = null;
		factory.MessageRepository.Reset();
		factory.Broadcaster.Reset();
		factory.ConversationUnicast.Reset();
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

	private static Message SeedMessage(ChatApiFactory f, long id, long channelId, long authorId)
	{
		var message = Message.Reconstitute(
			id: id, containerId: channelId, authorId: authorId, recipientId: null,
			content: "original", replyToId: null, editedAt: null,
			isDeleted: false, createdAt: DateTimeOffset.UtcNow);
		f.MessageRepository.Seed(message);
		return message;
	}

	private static Message SeedDirectMessage(ChatApiFactory f, long id, long conversationId, long authorId, long recipientId)
	{
		var message = Message.Reconstitute(
			id: id, containerId: conversationId, authorId: authorId, recipientId: recipientId,
			content: "original", replyToId: null, editedAt: null,
			isDeleted: false, createdAt: DateTimeOffset.UtcNow);
		f.MessageRepository.Seed(message);
		return message;
	}

	[Fact]
	public async Task Patch_WithoutToken_Returns401()
	{
		var response = await factory.CreateClient().PatchAsJsonAsync("/v1/messages/1",
			new { content = "hi" });

		Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
	}

	[Fact]
	public async Task Patch_HappyPath_Returns200WithEditedResponse()
	{
		SeedMessage(factory, id: 1, channelId: 100, authorId: 42);
		var client = BuildClient(userId: 42);

		var response = await client.PatchAsJsonAsync("/v1/messages/1",
			new { content = "updated content" });

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);

		var body = await response.Content.ReadFromJsonAsync<EditBody>(JsonOptions);
		Assert.NotNull(body);
		Assert.Equal("1", body.Id);
		Assert.Equal("updated content", body.Content);
		Assert.NotNull(body.EditedAt);

		var (channel, evt) = Assert.Single(factory.Broadcaster.EditedBroadcasts);
		Assert.Equal(100L, channel);
		Assert.Equal("1", evt.Id);
		Assert.Equal("updated content", evt.Content);
	}

	[Fact]
	public async Task Patch_MessageNotFound_Returns404()
	{
		var client = BuildClient(userId: 42);

		var response = await client.PatchAsJsonAsync("/v1/messages/999",
			new { content = "hi" });

		Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
	}

	[Fact]
	public async Task Patch_NotAuthor_Returns403()
	{
		SeedMessage(factory, id: 1, channelId: 100, authorId: 99);
		var client = BuildClient(userId: 42);

		var response = await client.PatchAsJsonAsync("/v1/messages/1",
			new { content = "hi" });

		Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
		Assert.Empty(factory.Broadcaster.EditedBroadcasts);
	}

	[Fact]
	public async Task Patch_EmptyContent_Returns400()
	{
		SeedMessage(factory, id: 1, channelId: 100, authorId: 42);
		var client = BuildClient(userId: 42);

		var response = await client.PatchAsJsonAsync("/v1/messages/1",
			new { content = "   " });

		Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
		Assert.Empty(factory.Broadcaster.EditedBroadcasts);
	}

	[Fact]
	public async Task Patch_Content4000Chars_Returns200()
	{
		SeedMessage(factory, id: 1, channelId: 100, authorId: 42);
		var client = BuildClient(userId: 42);

		var response = await client.PatchAsJsonAsync("/v1/messages/1",
			new { content = new string('a', 4000) });

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
	}

	[Fact]
	public async Task Patch_ContentOver4000Chars_Returns400()
	{
		SeedMessage(factory, id: 1, channelId: 100, authorId: 42);
		var client = BuildClient(userId: 42);

		var response = await client.PatchAsJsonAsync("/v1/messages/1",
			new { content = new string('a', 4001) });

		Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
		Assert.Empty(factory.Broadcaster.EditedBroadcasts);
	}

	[Fact]
	public async Task Patch_AlreadyDeletedMessage_Returns404()
	{
		var deleted = Message.Reconstitute(
			id: 1, containerId: 100, authorId: 42, recipientId: null,
			content: null, replyToId: null, editedAt: null,
			isDeleted: true, createdAt: DateTimeOffset.UtcNow);
		factory.MessageRepository.Seed(deleted);
		var client = BuildClient(userId: 42);

		var response = await client.PatchAsJsonAsync("/v1/messages/1",
			new { content = "new content" });

		Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
		Assert.Empty(factory.Broadcaster.EditedBroadcasts);
	}

	[Fact]
	public async Task Patch_EditTwice_Returns200WithUpdatedEditedAt()
	{
		SeedMessage(factory, id: 1, channelId: 100, authorId: 42);
		var client = BuildClient(userId: 42);

		var first  = await client.PatchAsJsonAsync("/v1/messages/1", new { content = "v1" });
		var second = await client.PatchAsJsonAsync("/v1/messages/1", new { content = "v2" });

		Assert.Equal(HttpStatusCode.OK, first.StatusCode);
		Assert.Equal(HttpStatusCode.OK, second.StatusCode);

		var body = await second.Content.ReadFromJsonAsync<EditBody>(JsonOptions);
		Assert.Equal("v2", body!.Content);
		Assert.Equal(2, factory.Broadcaster.EditedBroadcasts.Count);
	}

	[Fact]
	public async Task Patch_DirectMessage_HappyPath_Returns200AndUnicastsToRecipient_NotTheChannelBroadcaster()
	{
		SeedDirectMessage(factory, id: 1, conversationId: 555, authorId: 42, recipientId: 100);
		var client = BuildClient(userId: 42);

		var response = await client.PatchAsJsonAsync("/v1/messages/1",
			new { content = "updated dm content" });

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);

		var body = await response.Content.ReadFromJsonAsync<EditBody>(JsonOptions);
		Assert.NotNull(body);
		Assert.Equal("updated dm content", body.Content);

		var (recipientId, evt) = Assert.Single(factory.ConversationUnicast.EditedUnicasts);
		Assert.Equal(100L, recipientId);
		Assert.Equal("1", evt.Id);
		Assert.Equal("555", evt.ConversationId);

		// proves routing didn't leak into the channel path
		Assert.Empty(factory.Broadcaster.EditedBroadcasts);
	}

	[Fact]
	public async Task Patch_DirectMessage_NotAuthor_Returns403()
	{
		SeedDirectMessage(factory, id: 1, conversationId: 555, authorId: 99, recipientId: 42);
		var client = BuildClient(userId: 42);

		var response = await client.PatchAsJsonAsync("/v1/messages/1",
			new { content = "hi" });

		Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
		Assert.Empty(factory.ConversationUnicast.EditedUnicasts);
	}

	private sealed record EditBody(string Id, string? Content, DateTimeOffset? EditedAt);
}
