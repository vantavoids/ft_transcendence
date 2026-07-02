using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Chat.Domain.Attachments;
using Chat.Domain.Messages;
using Chat.FunctionalTests.Infrastructure;
using Xunit;

namespace Chat.FunctionalTests.Endpoints;

public sealed class AttachmentsEndpointTests(ChatApiFactory factory)
	: IClassFixture<ChatApiFactory>, IAsyncLifetime
{
	private const long ReadMessagesPermission = 1L << 1;

	// a 1x1 PNG; the exact bytes don't matter, only that the round trip is identical
	private static readonly byte[] PngBytes = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x01];

	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
		PropertyNameCaseInsensitive = true,
	};

	public Task InitializeAsync()
	{
		factory.GuildClient.Result = null;
		factory.AttachmentRepository.Reset();
		factory.ObjectStore.Reset();
		factory.MessageRepository.Reset();
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

	private static MultipartFormDataContent FilePart(byte[] bytes, string filename, string contentType)
	{
		var fileContent = new ByteArrayContent(bytes);
		fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
		return new MultipartFormDataContent { { fileContent, "file", filename } };
	}

	// ---- upload ----

	[Fact]
	public async Task Post_WithoutToken_Returns401()
	{
		var client = factory.CreateClient();

		var response = await client.PostAsync("/v1/attachments", FilePart(PngBytes, "pic.png", "image/png"));

		Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
	}

	[Fact]
	public async Task Post_HappyPath_Returns201_StoresBlobAndDraft()
	{
		var client = BuildClient(userId: 42);

		var response = await client.PostAsync("/v1/attachments", FilePart(PngBytes, "pic.png", "image/png"));

		Assert.Equal(HttpStatusCode.Created, response.StatusCode);
		var body = await response.Content.ReadFromJsonAsync<AttachmentBody>(JsonOptions);
		Assert.NotNull(body);
		Assert.Equal("pic.png", body.Filename);
		Assert.Equal("image/png", body.MimeType);
		Assert.Equal(PngBytes.Length, body.SizeBytes);
		Assert.True(long.TryParse(body.Id, out var id));

		// the blob landed in object storage under the snowflake key, and the draft
		// is owned by the uploader
		Assert.True(factory.ObjectStore.Objects.ContainsKey(id.ToString()));
		var draft = await factory.AttachmentRepository.GetDraftAsync(id, CancellationToken.None);
		Assert.NotNull(draft);
		Assert.Equal(42L, draft.UploaderId);
	}

	[Fact]
	public async Task Post_NoFile_Returns400()
	{
		var client = BuildClient(userId: 42);

		var response = await client.PostAsync("/v1/attachments", new MultipartFormDataContent());

		Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
	}

	[Fact]
	public async Task Post_BlockedMimeType_Returns415_StoresNothing()
	{
		var client = BuildClient(userId: 42);

		var response = await client.PostAsync("/v1/attachments",
			FilePart(Encoding.UTF8.GetBytes("#!/bin/sh\necho hi"), "run.sh", "application/x-sh"));

		Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
		Assert.Empty(factory.ObjectStore.Objects);
	}

	// ---- download: draft path (uploader-only) ----

	[Fact]
	public async Task Get_DraftByUploader_Returns200_InlineNosniff_ByteIdentical()
	{
		var client = BuildClient(userId: 42);
		var id = await UploadAsync(client, "pic.png", "image/png");

		var response = await client.GetAsync($"/v1/attachments/{id}/pic.png");

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
		Assert.Equal("inline", response.Content.Headers.ContentDisposition!.DispositionType);
		Assert.Equal(PngBytes, await response.Content.ReadAsByteArrayAsync());
	}

	[Fact]
	public async Task Get_DraftByAnotherUser_Returns403()
	{
		var owner = BuildClient(userId: 42);
		var id = await UploadAsync(owner, "pic.png", "image/png");

		var stranger = BuildClient(userId: 99);
		var response = await stranger.GetAsync($"/v1/attachments/{id}/pic.png");

		Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
	}

	[Fact]
	public async Task Get_WrongFilename_Returns404()
	{
		var client = BuildClient(userId: 42);
		var id = await UploadAsync(client, "pic.png", "image/png");

		var response = await client.GetAsync($"/v1/attachments/{id}/other.png");

		Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
	}

	[Fact]
	public async Task Get_UnknownId_Returns404()
	{
		var client = BuildClient(userId: 42);

		var response = await client.GetAsync("/v1/attachments/123456/pic.png");

		Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
	}

	// ---- download: channel path (membership-gated) ----

	[Fact]
	public async Task Get_ChannelAttachmentAsMember_Returns200()
	{
		const long attachmentId = 7001, channelId = 100, messageId = 5001;
		SeedChannelAttachment(attachmentId, channelId, messageId, "pic.png", "image/png");
		factory.WithMembership(channelId, userId: 42, guildId: 9, permissions: ReadMessagesPermission);

		var client = BuildClient(userId: 42);
		var response = await client.GetAsync($"/v1/attachments/{attachmentId}/pic.png");

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		Assert.Equal(PngBytes, await response.Content.ReadAsByteArrayAsync());
	}

	[Fact]
	public async Task Get_ChannelAttachmentAsNonMember_Returns403()
	{
		const long attachmentId = 7002, channelId = 100, messageId = 5002;
		SeedChannelAttachment(attachmentId, channelId, messageId, "pic.png", "image/png");
		factory.WithoutMembership();

		var client = BuildClient(userId: 99);
		var response = await client.GetAsync($"/v1/attachments/{attachmentId}/pic.png");

		Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
	}

	// ---- download: DM path (participant-gated) ----

	[Fact]
	public async Task Get_DmAttachmentAsSender_Returns200()
	{
		const long attachmentId = 8001, conversationId = 900, messageId = 6001;
		SeedDmAttachment(attachmentId, conversationId, messageId, senderId: 42, recipientId: 100, "pic.png", "image/png");

		var client = BuildClient(userId: 42);
		var response = await client.GetAsync($"/v1/attachments/{attachmentId}/pic.png");

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		Assert.Equal(PngBytes, await response.Content.ReadAsByteArrayAsync());
	}

	[Fact]
	public async Task Get_DmAttachmentAsRecipient_Returns200()
	{
		const long attachmentId = 8002, conversationId = 901, messageId = 6002;
		SeedDmAttachment(attachmentId, conversationId, messageId, senderId: 42, recipientId: 100, "pic.png", "image/png");

		var client = BuildClient(userId: 100);
		var response = await client.GetAsync($"/v1/attachments/{attachmentId}/pic.png");

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		Assert.Equal(PngBytes, await response.Content.ReadAsByteArrayAsync());
	}

	[Fact]
	public async Task Get_DmAttachmentAsUnrelatedUser_Returns403()
	{
		const long attachmentId = 8003, conversationId = 902, messageId = 6003;
		SeedDmAttachment(attachmentId, conversationId, messageId, senderId: 42, recipientId: 100, "pic.png", "image/png");

		var client = BuildClient(userId: 99);
		var response = await client.GetAsync($"/v1/attachments/{attachmentId}/pic.png");

		Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
	}

	private void SeedDmAttachment(
		long attachmentId, long conversationId, long messageId, long senderId, long recipientId, string filename, string mimeType)
	{
		var url = $"http://localhost/api/chat/v1/attachments/{attachmentId}/{filename}";
		factory.AttachmentRepository.SeedDmAttachment(conversationId, messageId,
			new AttachmentMetadata(attachmentId, url, filename, PngBytes.Length, mimeType));
		factory.ObjectStore.Seed(attachmentId.ToString(), PngBytes);

		var message = Message.Reconstitute(
			id: messageId, containerId: conversationId, authorId: senderId, recipientId: recipientId,
			content: "has an attachment", replyToId: null, editedAt: null, isDeleted: false,
			createdAt: DateTimeOffset.UtcNow);
		factory.MessageRepository.Seed(message);
	}

	private async Task<long> UploadAsync(HttpClient client, string filename, string contentType)
	{
		var response = await client.PostAsync("/v1/attachments", FilePart(PngBytes, filename, contentType));
		response.EnsureSuccessStatusCode();
		var body = await response.Content.ReadFromJsonAsync<AttachmentBody>(JsonOptions);
		return long.Parse(body!.Id);
	}

	private void SeedChannelAttachment(long attachmentId, long channelId, long messageId, string filename, string mimeType)
	{
		var url = $"http://localhost/api/chat/v1/attachments/{attachmentId}/{filename}";
		factory.AttachmentRepository.SeedChannelAttachment(channelId, messageId,
			new AttachmentMetadata(attachmentId, url, filename, PngBytes.Length, mimeType));
		factory.ObjectStore.Seed(attachmentId.ToString(), PngBytes);
	}

	private sealed record AttachmentBody(
		string Id,
		string Url,
		string Filename,
		long SizeBytes,
		string MimeType);
}
