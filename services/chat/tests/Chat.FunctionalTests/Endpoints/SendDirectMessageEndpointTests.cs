using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Chat.Application.Abstractions;
using Chat.Application.Contracts;
using Chat.FunctionalTests.Infrastructure;
using Xunit;

namespace Chat.FunctionalTests.Endpoints;

public sealed class SendDirectMessageEndpointTests(ChatApiFactory factory)
	: IClassFixture<ChatApiFactory>, IAsyncLifetime
{
	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
		PropertyNameCaseInsensitive = true,
	};

	public Task InitializeAsync()
	{
		factory.MessageRepository.Reset();
		factory.EventBus.Reset();
		factory.ConversationUnicast.Reset();
		factory.UserClient.Reset();
		// every test in this file sends as user 42; seed the relationship pairs
		// it exercises so the handler's blocked-check passes by default
		factory.UserClient.Setup(42, 100, new UsersRelationship("accepted", null));
		factory.UserClient.Setup(42, 42, new UsersRelationship("accepted", null));
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

		var response = await client.PostAsJsonAsync("/v1/dms/100/messages",
			new { content = "hi" });

		Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
	}

	[Fact]
	public async Task Post_HappyPath_Returns201_PersistsPublishesAndUnicasts()
	{
		var client = BuildClient(userId: 42);

		var response = await client.PostAsJsonAsync("/v1/dms/100/messages",
			new { content = "hello dm", nonce = "dm-nonce-1" });

		Assert.Equal(HttpStatusCode.Created, response.StatusCode);

		var body = await response.Content.ReadFromJsonAsync<DirectMessageBody>(JsonOptions);
		Assert.NotNull(body);
		Assert.Equal("42", body.SenderId);
		Assert.Equal("100", body.RecipientId);
		Assert.Equal("hello dm", body.Content);
		Assert.Null(body.ReplyToId);
		Assert.Equal("dm-nonce-1", body.Nonce);
		Assert.True(long.TryParse(body.Id, out var messageId));
		Assert.True(long.TryParse(body.ConversationId, out var conversationId));

		var saved = Assert.Single(factory.SavedDirectMessages);
		Assert.Equal(messageId, saved.Id);
		Assert.Equal(conversationId, saved.ContainerId);
		Assert.Equal(42L, saved.AuthorId);
		Assert.Equal(100L, saved.RecipientId);

		var evt = Assert.Single(factory.EventBus.PublishedOf<ChatDmSent>());
		Assert.Equal(messageId, evt.MessageId);
		Assert.Equal(conversationId, evt.ConversationId);
		Assert.Equal(42L, evt.SenderId);
		Assert.Equal(100L, evt.RecipientId);

		var unicastMessage = Assert.Single(factory.ConversationUnicast.Unicasts);
		Assert.Equal(body.Id, unicastMessage.Id);
		Assert.Equal("dm-nonce-1", unicastMessage.Nonce);
	}

	[Fact]
	public async Task Post_BlankContent_Returns400_NoSideEffects()
	{
		var client = BuildClient(userId: 42);

		var response = await client.PostAsJsonAsync("/v1/dms/100/messages",
			new { content = "   " });

		Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
		Assert.Empty(factory.SavedDirectMessages);
		Assert.Empty(factory.EventBus.Published);
		Assert.Empty(factory.ConversationUnicast.Unicasts);
	}

	[Fact]
	public async Task Post_ToSelf_Returns400_NoSideEffects()
	{
		var client = BuildClient(userId: 42);

		var response = await client.PostAsJsonAsync("/v1/dms/42/messages",
			new { content = "nope" });

		Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
		Assert.Empty(factory.SavedDirectMessages);
		Assert.Empty(factory.EventBus.Published);
		Assert.Empty(factory.ConversationUnicast.Unicasts);
	}

	[Fact]
	public async Task Post_InvalidReplyTarget_Returns400_NoSideEffects()
	{
		var client = BuildClient(userId: 42);

		var response = await client.PostAsJsonAsync("/v1/dms/100/messages",
			new { content = "reply", reply_to_id = 999 });

		Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
		Assert.Empty(factory.SavedDirectMessages);
		Assert.Empty(factory.EventBus.Published);
		Assert.Empty(factory.ConversationUnicast.Unicasts);
	}

	[Fact]
	public async Task Post_SameNonce_Returns201WithOriginalMessage()
	{
		var client = BuildClient(userId: 42);

		var first = await client.PostAsJsonAsync("/v1/dms/100/messages",
			new { content = "hello", nonce = "dedup-dm-nonce" });
		var firstBody = await first.Content.ReadFromJsonAsync<DirectMessageBody>(JsonOptions);

		var second = await client.PostAsJsonAsync("/v1/dms/100/messages",
			new { content = "hello", nonce = "dedup-dm-nonce" });
		var secondBody = await second.Content.ReadFromJsonAsync<DirectMessageBody>(JsonOptions);

		Assert.Equal(HttpStatusCode.Created, second.StatusCode);
		Assert.Equal(firstBody!.Id, secondBody!.Id);
		Assert.Equal(firstBody.CreatedAt, secondBody.CreatedAt);
		Assert.Equal("dedup-dm-nonce", secondBody.Nonce);
		Assert.Single(factory.SavedDirectMessages);
	}

	[Fact]
	public async Task Get_History_ReturnsLatestMessagesFirst()
	{
		var client = BuildClient(userId: 42);

		var first = await client.PostAsJsonAsync("/v1/dms/100/messages",
				new { content = "history functional test 1", nonce = "history-functional-1" });

		// the fake clock doesn't auto-advance like a real one would between
		// requests; advance it explicitly so each message gets a distinct
		// created_at and none of them collide with the list's default "now" cursor
		factory.Clock.Advance(TimeSpan.FromMilliseconds(1));

		var second = await client.PostAsJsonAsync("/v1/dms/100/messages",
				new { content = "history functional test 2", nonce = "history-functional-2" });

		Assert.Equal(HttpStatusCode.Created, first.StatusCode);
		Assert.Equal(HttpStatusCode.Created, second.StatusCode);

		factory.Clock.Advance(TimeSpan.FromMilliseconds(1));

		var response = await client.GetAsync("/v1/dms/100/messages?limit=50");

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);

		var messages = await response.Content.ReadFromJsonAsync<List<DirectMessageBody>>(JsonOptions);

		Assert.NotNull(messages);
		Assert.Equal(2, messages!.Count);

		Assert.Equal("history functional test 2", messages[0].Content);
		Assert.Equal("history functional test 1", messages[1].Content);

		Assert.Equal(messages[0].ConversationId, messages[1].ConversationId);
		Assert.Equal("42", messages[0].SenderId);
		Assert.Equal("100", messages[0].RecipientId);
		Assert.Null(messages[0].Nonce);
	}

	[Fact]
	public async Task Get_HistoryWithoutConversation_ReturnsEmptyList()
	{
		var client = BuildClient(userId: 42);

		var response = await client.GetAsync("/v1/dms/999/messages?limit=50");

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);

		var messages = await response.Content.ReadFromJsonAsync<List<DirectMessageBody>>(JsonOptions);

		Assert.NotNull(messages);
		Assert.Empty(messages!);
	}

	[Fact]
	public async Task Get_HistoryWithSelf_Returns400()
	{
		var client = BuildClient(userId: 42);

		var response = await client.GetAsync("/v1/dms/42/messages?limit=50");

		Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
	}

	// a 1x1 PNG; the exact bytes don't matter, only that a draft can be resolved
	private static readonly byte[] PngBytes = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x01];

	private async Task<string> UploadDraftAsync(HttpClient client)
	{
		var fileContent = new ByteArrayContent(PngBytes);
		fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
		var form = new MultipartFormDataContent { { fileContent, "file", "pic.png" } };

		var response = await client.PostAsync("/v1/attachments", form);
		response.EnsureSuccessStatusCode();
		var body = await response.Content.ReadFromJsonAsync<UploadedAttachmentBody>(JsonOptions);
		return body!.Id;
	}

	[Fact]
	public async Task Post_WithAttachment_Returns201WithAttachment()
	{
		var client = BuildClient(userId: 42);
		var attachmentId = await UploadDraftAsync(client);

		var response = await client.PostAsJsonAsync("/v1/dms/100/messages",
			new { content = "look at this", attachment_ids = new[] { attachmentId } });

		Assert.Equal(HttpStatusCode.Created, response.StatusCode);
		var body = await response.Content.ReadFromJsonAsync<DirectMessageBody>(JsonOptions);
		Assert.NotNull(body);
		var attachment = Assert.Single(body.Attachments);
		Assert.Equal(attachmentId, attachment.Id);
		Assert.Equal("pic.png", attachment.Filename);
	}

	[Fact]
	public async Task Post_AttachmentOnly_NoContent_Returns201()
	{
		var client = BuildClient(userId: 42);
		var attachmentId = await UploadDraftAsync(client);

		var response = await client.PostAsJsonAsync("/v1/dms/100/messages",
			new { content = (string?)null, attachment_ids = new[] { attachmentId } });

		Assert.Equal(HttpStatusCode.Created, response.StatusCode);
		var body = await response.Content.ReadFromJsonAsync<DirectMessageBody>(JsonOptions);
		Assert.NotNull(body);
		Assert.Single(body.Attachments);
	}

	[Fact]
	public async Task Post_AttachmentOwnedByAnotherUser_Returns404_NoSideEffects()
	{
		var uploader = BuildClient(userId: 100);
		var attachmentId = await UploadDraftAsync(uploader);

		var sender = BuildClient(userId: 42);
		var response = await sender.PostAsJsonAsync("/v1/dms/100/messages",
			new { content = "sneaky", attachment_ids = new[] { attachmentId } });

		Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
		Assert.Empty(factory.SavedDirectMessages);
	}

	private sealed record UploadedAttachmentBody(string Id, string Url, string Filename, long SizeBytes, string MimeType);

	private sealed record AttachmentBody(string Id, string Url, string Filename, long SizeBytes, string MimeType);

	private sealed record DirectMessageBody(
			string Id,
			string ConversationId,
			string SenderId,
			string RecipientId,
			string? Content,
			string? ReplyToId,
			DateTimeOffset? EditedAt,
			DateTimeOffset CreatedAt,
			IReadOnlyList<AttachmentBody> Attachments,
			string? Nonce);
}
