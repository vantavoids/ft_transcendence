using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Chat.Application.Features.DirectMessages.Common;
using Chat.Domain.Messages;
using Chat.FunctionalTests.Infrastructure;
using Xunit;

namespace Chat.FunctionalTests.Endpoints;

public sealed class UpdateDmReadStateEndpointTests(ChatApiFactory factory)
	: IClassFixture<ChatApiFactory>, IAsyncLifetime
{
	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
		PropertyNameCaseInsensitive = true,
	};

	private static readonly DateTimeOffset PastDate = new(2025, 6, 1, 12, 0, 0, TimeSpan.Zero);

	public Task InitializeAsync()
	{
		factory.MessageRepository.Reset();
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
	public async Task Put_WithoutToken_Returns401()
	{
		var response = await factory.CreateClient().PutAsJsonAsync("/v1/dms/100/read", new { message_id = 1 });

		Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
	}

	[Fact]
	public async Task Put_MissingMessageId_Returns400()
	{
		var client = BuildClient(userId: 42);

		var response = await client.PutAsJsonAsync("/v1/dms/100/read", new { });

		Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
	}

	[Fact]
	public async Task Put_NoConversation_Returns404()
	{
		var client = BuildClient(userId: 42);

		var response = await client.PutAsJsonAsync("/v1/dms/100/read", new { message_id = 1 });

		Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
	}

	[Fact]
	public async Task Put_MessageNotFound_Returns404()
	{
		factory.MessageRepository.WithConversation(42, 100, conversationId: 555);
		var client = BuildClient(userId: 42);

		var response = await client.PutAsJsonAsync("/v1/dms/100/read", new { message_id = 999 });

		Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
	}

	[Fact]
	public async Task Put_HappyPath_Returns200_AdvancesCursor_ResetsUnread()
	{
		factory.MessageRepository.WithConversation(42, 100, conversationId: 555);
		factory.MessageRepository.Seed(Message.Reconstitute(
			id: 1, containerId: 555, authorId: 100, recipientId: 42,
			content: "hi", replyToId: null, editedAt: null, isDeleted: false, createdAt: PastDate));
		var client = BuildClient(userId: 42);

		var response = await client.PutAsJsonAsync("/v1/dms/100/read", new { message_id = 1 });

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		var body = await response.Content.ReadFromJsonAsync<DmReadStateResponse>(JsonOptions);
		Assert.NotNull(body);
		Assert.Equal("100", body.PartnerId);
		Assert.Equal("1", body.LastReadMessageId);
		Assert.Equal(0, body.UnreadCount);
	}
}
