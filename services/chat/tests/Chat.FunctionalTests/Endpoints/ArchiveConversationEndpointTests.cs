using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Chat.FunctionalTests.Infrastructure;
using Chat.Domain.Conversations;
using Xunit;

namespace Chat.FunctionalTests.Endpoints;

public sealed class ArchiveConversationEndpointTests(ChatApiFactory factory)
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
	public async Task Delete_WithoutToken_Returns401()
	{
		var client = factory.CreateClient();

		var response = await client.DeleteAsync("/v1/dms/100");

		Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
	}

	[Fact]
	public async Task Delete_NoConversation_Returns404()
	{
		var client = BuildClient(userId: 42);

		var response = await client.DeleteAsync("/v1/dms/999");

		Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
	}

	[Fact]
	public async Task Delete_HappyPath_Returns204_ArchivesCallerSideOnly()
	{
		factory.MessageRepository.WithConversation(42, 100, conversationId: 555);
		factory.MessageRepository.WithConversationSummary(42, new DmConversation(
			PartnerId: 100, LastMessageAt: DateTimeOffset.UtcNow, LastPreview: "hey", IsArchived: false));
		factory.MessageRepository.WithConversationSummary(100, new DmConversation(
			PartnerId: 42, LastMessageAt: DateTimeOffset.UtcNow, LastPreview: "hey", IsArchived: false));

		var client = BuildClient(userId: 42);
		var response = await client.DeleteAsync("/v1/dms/100");

		Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

		var callerSide = Assert.Single(await factory.MessageRepository.GetConversationsAsync(42, default));
		Assert.True(callerSide.IsArchived);

		var partnerSide = Assert.Single(await factory.MessageRepository.GetConversationsAsync(100, default));
		Assert.False(partnerSide.IsArchived);
	}

	[Fact]
	public async Task Delete_AlreadyArchived_IsIdempotent_Returns204()
	{
		factory.MessageRepository.WithConversation(42, 100, conversationId: 555);
		factory.MessageRepository.WithConversationSummary(42, new DmConversation(
			PartnerId: 100, LastMessageAt: DateTimeOffset.UtcNow, LastPreview: "hey", IsArchived: true));

		var client = BuildClient(userId: 42);
		var response = await client.DeleteAsync("/v1/dms/100");

		Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
	}

	[Fact]
	public async Task Delete_ThenGetDms_HidesArchivedConversation_UntilIncludeArchivedRequested()
	{
		factory.MessageRepository.WithConversation(42, 100, conversationId: 555);
		factory.MessageRepository.WithConversationSummary(42, new DmConversation(
			PartnerId: 100, LastMessageAt: DateTimeOffset.UtcNow, LastPreview: "hey", IsArchived: false));

		var client = BuildClient(userId: 42);

		var archiveResponse = await client.DeleteAsync("/v1/dms/100");
		Assert.Equal(HttpStatusCode.NoContent, archiveResponse.StatusCode);

		var listResponse = await client.GetAsync("/v1/dms");
		var listBody = await listResponse.Content.ReadFromJsonAsync<List<DmConversationBody>>(JsonOptions);
		Assert.NotNull(listBody);
		Assert.Empty(listBody!);

		var listWithArchivedResponse = await client.GetAsync("/v1/dms?include_archived=true");
		var listWithArchivedBody = await listWithArchivedResponse.Content.ReadFromJsonAsync<List<DmConversationBody>>(JsonOptions);
		Assert.NotNull(listWithArchivedBody);
		var conversation = Assert.Single(listWithArchivedBody!);
		Assert.True(conversation.IsArchived);
	}

	private sealed record DmConversationBody(
		string PartnerId,
		string? LastPreview,
		DateTimeOffset LastMessageAt,
		int UnreadCount,
		bool IsArchived);
}
