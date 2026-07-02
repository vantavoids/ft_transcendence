using System.Net;
using System.Net.Http.Headers;
using Chat.Application.Abstractions;
using Chat.Domain.Messages;
using Chat.FunctionalTests.Infrastructure;
using Xunit;

namespace Chat.FunctionalTests.Endpoints;

public sealed class DeleteMessageEndpointTests(ChatApiFactory factory)
	: IClassFixture<ChatApiFactory>, IAsyncLifetime
{
	private const long ManageMessagesPermission = 1L << 2;

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

	private static void SeedMessage(ChatApiFactory f, long id, long channelId, long authorId)
	{
		var message = Message.Reconstitute(
			id: id, containerId: channelId, authorId: authorId, recipientId: null,
			content: "hello", replyToId: null, editedAt: null,
			isDeleted: false, createdAt: DateTimeOffset.UtcNow);
		f.MessageRepository.Seed(message);
	}

	[Fact]
	public async Task Delete_WithoutToken_Returns401()
	{
		var response = await factory.CreateClient().DeleteAsync("/v1/messages/1");

		Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
	}

	[Fact]
	public async Task Delete_Author_Returns204AndBroadcasts()
	{
		SeedMessage(factory, id: 1, channelId: 100, authorId: 42);
		var client = BuildClient(userId: 42);

		var response = await client.DeleteAsync("/v1/messages/1");

		Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

		var (channel, messageId) = Assert.Single(factory.Broadcaster.DeletedBroadcasts);
		Assert.Equal(100L, channel);
		Assert.Equal(1L, messageId);
	}

	[Fact]
	public async Task Delete_MessageNotFound_Returns404()
	{
		var client = BuildClient(userId: 42);

		var response = await client.DeleteAsync("/v1/messages/999");

		Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
	}

	[Fact]
	public async Task Delete_NonMember_Returns404()
	{
		SeedMessage(factory, id: 1, channelId: 100, authorId: 99);
		factory.GuildClient.Result = new ChannelMembership(IsMember: false, GuildId: 5, Permissions: 0);
		var client = BuildClient(userId: 42);

		var response = await client.DeleteAsync("/v1/messages/1");

		Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
		Assert.Empty(factory.Broadcaster.DeletedBroadcasts);
	}

	[Fact]
	public async Task Delete_MemberWithoutManageMessages_Returns403()
	{
		SeedMessage(factory, id: 1, channelId: 100, authorId: 99);
		factory.GuildClient.Result = new ChannelMembership(IsMember: true, GuildId: 5, Permissions: 0);
		var client = BuildClient(userId: 42);

		var response = await client.DeleteAsync("/v1/messages/1");

		Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
		Assert.Empty(factory.Broadcaster.DeletedBroadcasts);
	}

	[Fact]
	public async Task Delete_MemberWithManageMessages_Returns204()
	{
		SeedMessage(factory, id: 1, channelId: 100, authorId: 99);
		factory.GuildClient.Result = new ChannelMembership(IsMember: true, GuildId: 5, Permissions: ManageMessagesPermission);
		var client = BuildClient(userId: 42);

		var response = await client.DeleteAsync("/v1/messages/1");

		Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
		Assert.Single(factory.Broadcaster.DeletedBroadcasts);
	}

	[Fact]
	public async Task Delete_MemberWithAdministrator_Returns204()
	{
		const long administratorPermission = 1L << 8;
		SeedMessage(factory, id: 1, channelId: 100, authorId: 99);
		factory.GuildClient.Result = new ChannelMembership(IsMember: true, GuildId: 5, Permissions: administratorPermission);
		var client = BuildClient(userId: 42);

		var response = await client.DeleteAsync("/v1/messages/1");

		Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
		Assert.Single(factory.Broadcaster.DeletedBroadcasts);
	}

	[Fact]
	public async Task Delete_AlreadyDeletedMessage_Returns404()
	{
		var deleted = Message.Reconstitute(
			id: 1, containerId: 100, authorId: 42, recipientId: null,
			content: null, replyToId: null, editedAt: null,
			isDeleted: true, createdAt: DateTimeOffset.UtcNow);
		factory.MessageRepository.Seed(deleted);
		var client = BuildClient(userId: 42);

		var response = await client.DeleteAsync("/v1/messages/1");

		Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
		Assert.Empty(factory.Broadcaster.DeletedBroadcasts);
	}
}
