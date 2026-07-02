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
			id: id, containerId: channelId, authorId: 99, recipientId: null,
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

	// T3.14b — ADMINISTRATOR bit grants read access without explicit READ_MESSAGES
	[Fact]
	public async Task Get_AdministratorPermission_Returns200()
	{
		const long administrator = 1L << 8;
		factory.GuildClient.Result = new Chat.Application.Abstractions.ChannelMembership(
			IsMember: true, GuildId: 5, Permissions: administrator);
		var client = BuildClient(userId: 42);

		var response = await client.GetAsync("/v1/channels/100/messages");

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
	}

	// T3.11 — channel with no messages returns empty array
	[Fact]
	public async Task Get_EmptyChannel_ReturnsEmptyArray()
	{
		factory.GuildClient.Result = new Chat.Application.Abstractions.ChannelMembership(
			IsMember: true, GuildId: 5, Permissions: ReadMessagesPermission);
		var client = BuildClient(userId: 42);

		var response = await client.GetAsync("/v1/channels/100/messages");

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		var messages = await response.Content.ReadFromJsonAsync<List<MessageResponse>>(JsonOptions);
		Assert.Empty(messages!);
	}

	// T3.02 — default limit is 50 when more messages exist
	[Fact]
	public async Task Get_DefaultLimit_Returns50Messages()
	{
		factory.GuildClient.Result = new Chat.Application.Abstractions.ChannelMembership(
			IsMember: true, GuildId: 5, Permissions: ReadMessagesPermission);
		for (var i = 1; i <= 60; i++)
			SeedMessage(factory, id: i, channelId: 100, createdAt: PastDate.AddMinutes(-i));
		var client = BuildClient(userId: 42);

		var response = await client.GetAsync("/v1/channels/100/messages");

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		var messages = await response.Content.ReadFromJsonAsync<List<MessageResponse>>(JsonOptions);
		Assert.Equal(50, messages!.Count);
	}

	// T3.03 — explicit limit=10 returns exactly 10 messages
	[Fact]
	public async Task Get_ExplicitLimit_ReturnsExactCount()
	{
		factory.GuildClient.Result = new Chat.Application.Abstractions.ChannelMembership(
			IsMember: true, GuildId: 5, Permissions: ReadMessagesPermission);
		for (var i = 1; i <= 20; i++)
			SeedMessage(factory, id: i, channelId: 100, createdAt: PastDate.AddMinutes(-i));
		var client = BuildClient(userId: 42);

		var response = await client.GetAsync("/v1/channels/100/messages?limit=10");

		var messages = await response.Content.ReadFromJsonAsync<List<MessageResponse>>(JsonOptions);
		Assert.Equal(10, messages!.Count);
	}

	// T3.04 & T3.06 — limit below 1 is clamped to 1
	[Theory]
	[InlineData(0)]
	[InlineData(-5)]
	public async Task Get_LimitBelowMin_IsClampedToOne(int limitValue)
	{
		factory.GuildClient.Result = new Chat.Application.Abstractions.ChannelMembership(
			IsMember: true, GuildId: 5, Permissions: ReadMessagesPermission);
		for (var i = 1; i <= 5; i++)
			SeedMessage(factory, id: i, channelId: 100, createdAt: PastDate.AddMinutes(-i));
		var client = BuildClient(userId: 42);

		var response = await client.GetAsync($"/v1/channels/100/messages?limit={limitValue}");

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		var messages = await response.Content.ReadFromJsonAsync<List<MessageResponse>>(JsonOptions);
		Assert.Single(messages!);
	}

	// T3.05 — limit above 100 is clamped to 100
	[Fact]
	public async Task Get_LimitAboveMax_IsClampedTo100()
	{
		factory.GuildClient.Result = new Chat.Application.Abstractions.ChannelMembership(
			IsMember: true, GuildId: 5, Permissions: ReadMessagesPermission);
		for (var i = 1; i <= 150; i++)
			SeedMessage(factory, id: i, channelId: 100, createdAt: PastDate.AddMinutes(-i));
		var client = BuildClient(userId: 42);

		var response = await client.GetAsync("/v1/channels/100/messages?limit=200");

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		var messages = await response.Content.ReadFromJsonAsync<List<MessageResponse>>(JsonOptions);
		Assert.Equal(100, messages!.Count);
	}

	// T3.08 — before_time far in the past returns empty array
	[Fact]
	public async Task Get_BeforeTimeFarInPast_ReturnsEmpty()
	{
		factory.GuildClient.Result = new Chat.Application.Abstractions.ChannelMembership(
			IsMember: true, GuildId: 5, Permissions: ReadMessagesPermission);
		SeedMessage(factory, id: 1, channelId: 100);
		var client = BuildClient(userId: 42);

		var farPast = Uri.EscapeDataString(
			new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero).ToString("O"));
		var response = await client.GetAsync($"/v1/channels/100/messages?before_time={farPast}");

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		var messages = await response.Content.ReadFromJsonAsync<List<MessageResponse>>(JsonOptions);
		Assert.Empty(messages!);
	}

	// T3.16 — messages are ordered by created_at DESC (newest first)
	[Fact]
	public async Task Get_Messages_AreReturnedNewestFirst()
	{
		factory.GuildClient.Result = new Chat.Application.Abstractions.ChannelMembership(
			IsMember: true, GuildId: 5, Permissions: ReadMessagesPermission);
		SeedMessage(factory, id: 1, channelId: 100, createdAt: PastDate.AddMinutes(-30));
		SeedMessage(factory, id: 2, channelId: 100, createdAt: PastDate.AddMinutes(-20));
		SeedMessage(factory, id: 3, channelId: 100, createdAt: PastDate.AddMinutes(-10));
		var client = BuildClient(userId: 42);

		var response = await client.GetAsync("/v1/channels/100/messages");

		var messages = await response.Content.ReadFromJsonAsync<List<MessageResponse>>(JsonOptions);
		Assert.Equal(3, messages!.Count);
		Assert.Equal("3", messages[0].Id);
		Assert.Equal("2", messages[1].Id);
		Assert.Equal("1", messages[2].Id);
	}

	// T3.17 — editedAt is populated for edited messages
	[Fact]
	public async Task Get_EditedMessage_HasEditedAtPopulated()
	{
		factory.GuildClient.Result = new Chat.Application.Abstractions.ChannelMembership(
			IsMember: true, GuildId: 5, Permissions: ReadMessagesPermission);
		var edited = Message.Reconstitute(
			id: 1, containerId: 100, authorId: 99, recipientId: null,
			content: "edited", replyToId: null,
			editedAt: PastDate.AddMinutes(-5),
			isDeleted: false, createdAt: PastDate.AddMinutes(-10));
		factory.MessageRepository.Seed(edited);
		var client = BuildClient(userId: 42);

		var response = await client.GetAsync("/v1/channels/100/messages");

		var messages = await response.Content.ReadFromJsonAsync<List<MessageResponse>>(JsonOptions);
		var msg = Assert.Single(messages!);
		Assert.NotNull(msg.EditedAt);
	}

	// T3.18 — nonce is always null in history responses
	[Fact]
	public async Task Get_Messages_NonceIsAlwaysNull()
	{
		factory.GuildClient.Result = new Chat.Application.Abstractions.ChannelMembership(
			IsMember: true, GuildId: 5, Permissions: ReadMessagesPermission);
		SeedMessage(factory, id: 1, channelId: 100);
		var client = BuildClient(userId: 42);

		var response = await client.GetAsync("/v1/channels/100/messages");

		var messages = await response.Content.ReadFromJsonAsync<List<MessageResponse>>(JsonOptions);
		Assert.All(messages!, m => Assert.Null(m.Nonce));
	}
}
