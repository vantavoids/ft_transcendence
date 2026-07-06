using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Chat.Application.Abstractions;
using Chat.Application.Features.Channels.Common;
using Chat.Domain.Messages;
using Chat.FunctionalTests.Infrastructure;
using Xunit;

namespace Chat.FunctionalTests.Endpoints;

public sealed class UpdateChannelReadStateEndpointTests(ChatApiFactory factory)
	: IClassFixture<ChatApiFactory>, IAsyncLifetime
{
	private const long ReadMessagesPermission = 1L << 1;

	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
		PropertyNameCaseInsensitive = true,
	};

	private static readonly DateTimeOffset PastDate = new(2025, 6, 1, 12, 0, 0, TimeSpan.Zero);

	public Task InitializeAsync()
	{
		factory.GuildClient.Result = null;
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

	private static void SeedMessage(ChatApiFactory f, long id, long channelId, DateTimeOffset? createdAt = null)
	{
		var message = Message.Reconstitute(
			id: id, containerId: channelId, authorId: 99, recipientId: null,
			content: "hello", replyToId: null, editedAt: null,
			isDeleted: false, createdAt: createdAt ?? PastDate);
		f.MessageRepository.Seed(message);
	}

	[Fact]
	public async Task Put_WithoutToken_Returns401()
	{
		var response = await factory.CreateClient().PutAsJsonAsync("/v1/channels/100/read", new { message_id = 1 });

		Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
	}

	[Fact]
	public async Task Put_MissingMessageId_Returns400()
	{
		var client = BuildClient(userId: 42);

		var response = await client.PutAsJsonAsync("/v1/channels/100/read", new { });

		Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
	}

	[Fact]
	public async Task Put_ChannelNotFound_Returns404()
	{
		factory.GuildClient.Result = null;
		var client = BuildClient(userId: 42);

		var response = await client.PutAsJsonAsync("/v1/channels/100/read", new { message_id = 1 });

		Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
	}

	[Fact]
	public async Task Put_NotAMember_Returns403()
	{
		factory.GuildClient.Result = new ChannelMembership(IsMember: false, GuildId: 5, Permissions: ReadMessagesPermission);
		var client = BuildClient(userId: 42);

		var response = await client.PutAsJsonAsync("/v1/channels/100/read", new { message_id = 1 });

		Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
	}

	[Fact]
	public async Task Put_MessageNotFound_Returns404()
	{
		factory.GuildClient.Result = new ChannelMembership(IsMember: true, GuildId: 5, Permissions: ReadMessagesPermission);
		var client = BuildClient(userId: 42);

		var response = await client.PutAsJsonAsync("/v1/channels/100/read", new { message_id = 999 });

		Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
	}

	[Fact]
	public async Task Put_HappyPath_Returns200_AdvancesCursor()
	{
		factory.GuildClient.Result = new ChannelMembership(IsMember: true, GuildId: 5, Permissions: ReadMessagesPermission);
		SeedMessage(factory, id: 1, channelId: 100);
		var client = BuildClient(userId: 42);

		var response = await client.PutAsJsonAsync("/v1/channels/100/read", new { message_id = 1 });

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		var body = await response.Content.ReadFromJsonAsync<ChannelReadStateResponse>(JsonOptions);
		Assert.NotNull(body);
		Assert.Equal("100", body.ChannelId);
		Assert.Equal("1", body.LastReadMessageId);
		Assert.Equal(0, body.UnreadCount);
	}
}
