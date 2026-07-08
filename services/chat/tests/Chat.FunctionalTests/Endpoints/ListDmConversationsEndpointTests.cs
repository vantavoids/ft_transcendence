using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Chat.FunctionalTests.Infrastructure;
using Chat.Domain.Conversations;
using Xunit;

namespace Chat.FunctionalTests.Endpoints;

public sealed class ListDmConversationsEndpointTests(ChatApiFactory factory)
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
		var client = factory.CreateClient();

		var response = await client.GetAsync("/v1/dms");

		Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
	}

	[Fact]
	public async Task Get_NoConversations_Returns200WithEmptyArray()
	{
		var client = BuildClient(userId: 42);

		var response = await client.GetAsync("/v1/dms");

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);

		var body = await response.Content.ReadFromJsonAsync<List<DmConversationBody>>(JsonOptions);
		Assert.NotNull(body);
		Assert.Empty(body!);
	}

	[Fact]
	public async Task Get_HappyPath_ReturnsConversations_NewestFirst()
	{
		var anchor = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

		factory.MessageRepository.WithConversationSummary(42, new DmConversation(
			PartnerId: 100, LastMessageAt: anchor.AddMinutes(-10), LastPreview: "older chat", IsArchived: false));
		factory.MessageRepository.WithConversationSummary(42, new DmConversation(
			PartnerId: 200, LastMessageAt: anchor, LastPreview: "newest chat", IsArchived: false));

		var client = BuildClient(userId: 42);
		var response = await client.GetAsync("/v1/dms");

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);

		var body = await response.Content.ReadFromJsonAsync<List<DmConversationBody>>(JsonOptions);
		Assert.NotNull(body);
		Assert.Equal(2, body!.Count);

		Assert.Equal("200", body[0].PartnerId);
		Assert.Equal("newest chat", body[0].LastPreview);
		Assert.Equal(0, body[0].UnreadCount);
		Assert.False(body[0].IsArchived);

		Assert.Equal("100", body[1].PartnerId);
	}

	[Fact]
	public async Task Get_ExcludesArchived_ByDefault()
	{
		var anchor = DateTimeOffset.UtcNow;

		factory.MessageRepository.WithConversationSummary(42, new DmConversation(
			PartnerId: 100, LastMessageAt: anchor, LastPreview: "open", IsArchived: false));
		factory.MessageRepository.WithConversationSummary(42, new DmConversation(
			PartnerId: 200, LastMessageAt: anchor, LastPreview: "closed", IsArchived: true));

		var client = BuildClient(userId: 42);
		var response = await client.GetAsync("/v1/dms");

		var body = await response.Content.ReadFromJsonAsync<List<DmConversationBody>>(JsonOptions);
		Assert.NotNull(body);
		var conversation = Assert.Single(body!);
		Assert.Equal("100", conversation.PartnerId);
	}

	[Fact]
	public async Task Get_IncludeArchivedTrue_ReturnsBoth()
	{
		var anchor = DateTimeOffset.UtcNow;

		factory.MessageRepository.WithConversationSummary(42, new DmConversation(
			PartnerId: 100, LastMessageAt: anchor, LastPreview: "open", IsArchived: false));
		factory.MessageRepository.WithConversationSummary(42, new DmConversation(
			PartnerId: 200, LastMessageAt: anchor, LastPreview: "closed", IsArchived: true));

		var client = BuildClient(userId: 42);
		var response = await client.GetAsync("/v1/dms?include_archived=true");

		var body = await response.Content.ReadFromJsonAsync<List<DmConversationBody>>(JsonOptions);
		Assert.NotNull(body);
		Assert.Equal(2, body!.Count);
		Assert.Contains(body, c => c.PartnerId == "200" && c.IsArchived);
	}

	[Fact]
	public async Task Get_UnreadCount_ReflectsDmReadStateRepository_PerPartner()
	{
		var anchor = DateTimeOffset.UtcNow;

		factory.MessageRepository.WithConversationSummary(42, new DmConversation(
			PartnerId: 100, LastMessageAt: anchor, LastPreview: "hey", IsArchived: false));
		factory.MessageRepository.WithConversationSummary(42, new DmConversation(
			PartnerId: 200, LastMessageAt: anchor, LastPreview: "yo", IsArchived: false));
		factory.ReadStateRepository.SeedDmUnreadCount(userId: 42, partnerId: 100, count: 5);

		var client = BuildClient(userId: 42);
		var response = await client.GetAsync("/v1/dms");

		var body = await response.Content.ReadFromJsonAsync<List<DmConversationBody>>(JsonOptions);
		Assert.NotNull(body);
		Assert.Equal(5, body!.Single(c => c.PartnerId == "100").UnreadCount);
		Assert.Equal(0, body!.Single(c => c.PartnerId == "200").UnreadCount);
	}

	[Fact]
	public async Task Get_OnlyReturnsCallersOwnConversations()
	{
		factory.MessageRepository.WithConversationSummary(42, new DmConversation(
			PartnerId: 100, LastMessageAt: DateTimeOffset.UtcNow, LastPreview: "mine", IsArchived: false));
		factory.MessageRepository.WithConversationSummary(999, new DmConversation(
			PartnerId: 100, LastMessageAt: DateTimeOffset.UtcNow, LastPreview: "not mine", IsArchived: false));

		var client = BuildClient(userId: 42);
		var response = await client.GetAsync("/v1/dms");

		var body = await response.Content.ReadFromJsonAsync<List<DmConversationBody>>(JsonOptions);
		Assert.NotNull(body);
		var conversation = Assert.Single(body!);
		Assert.Equal("mine", conversation.LastPreview);
	}
}

internal sealed record DmConversationBody(
	string PartnerId,
	string? LastPreview,
	DateTimeOffset LastMessageAt,
	int UnreadCount,
	bool IsArchived);
