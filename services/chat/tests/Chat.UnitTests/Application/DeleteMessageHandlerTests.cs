using Chat.Application.Abstractions;
using Chat.Application.Abstractions.Messaging;
using Chat.Application.Features.Messages.DeleteMessage;
using Chat.Domain.Messages;
using Chat.Domain.Results;
using Chat.UnitTests.Fakes;
using Xunit;

namespace Chat.UnitTests.Application;

public sealed class DeleteMessageHandlerTests
{
	private const long ManageMessagesPermission = 1L << 2;
	private const long AdministratorPermission = 1L << 8;

	private sealed record Harness(
		FakeCurrentUser CurrentUser,
		FakeGuildClient GuildClient,
		FakeMessageRepository Repository,
		FakeChannelBroadcaster Broadcaster);

	private static (Harness Harness, ICommandHandler<DeleteMessageCommand, Result> Handler)
		BuildHandler(long userId = 42)
	{
		var currentUser = new FakeCurrentUser { UserId = userId };
		var guildClient = new FakeGuildClient();
		var repository = new FakeMessageRepository();
		var broadcaster = new FakeChannelBroadcaster();

		var handler = HandlerFactory.CreateCommand<DeleteMessageCommand, Result>(
			currentUser, guildClient, repository, broadcaster);

		return (new Harness(currentUser, guildClient, repository, broadcaster), handler);
	}

	private static Message Seed(FakeMessageRepository repo, long id = 1, long channelId = 100, long authorId = 42, bool isDeleted = false)
	{
		var message = Message.Reconstitute(
			id: id, containerId: channelId, authorId: authorId, recipientId: null,
			content: "hello", replyToId: null, editedAt: null,
			isDeleted: isDeleted, createdAt: DateTimeOffset.UtcNow);
		repo.Seed(message);
		return message;
	}

	[Fact]
	public async Task MessageNotFound_ReturnsNotFound_NoSideEffects()
	{
		var (h, handler) = BuildHandler();

		var result = await handler.HandleAsync(new DeleteMessageCommand(MessageId: 999));

		Assert.True(result.IsFailure);
		Assert.Equal(MessageFailures.NotFound, result.Error);
		Assert.Empty(h.Repository.SoftDeleted);
		Assert.Empty(h.Broadcaster.DeletedBroadcasts);
	}

	[Fact]
	public async Task AlreadyDeleted_ReturnsNotFound_NoSideEffects()
	{
		var (h, handler) = BuildHandler(userId: 42);
		Seed(h.Repository, authorId: 42, isDeleted: true);

		var result = await handler.HandleAsync(new DeleteMessageCommand(MessageId: 1));

		Assert.True(result.IsFailure);
		Assert.Equal(MessageFailures.NotFound, result.Error);
		Assert.Empty(h.Repository.SoftDeleted);
	}

	[Fact]
	public async Task Author_DeletesWithoutPermissionCheck()
	{
		var (h, handler) = BuildHandler(userId: 42);
		Seed(h.Repository, authorId: 42);

		var result = await handler.HandleAsync(new DeleteMessageCommand(MessageId: 1));

		Assert.True(result.Succeeded);
		Assert.Single(h.Repository.SoftDeleted);
		Assert.Single(h.Broadcaster.DeletedBroadcasts);
		Assert.Null(h.GuildClient.Result);
	}

	[Fact]
	public async Task NonAuthor_NonMember_ReturnsNotFound()
	{
		var (h, handler) = BuildHandler(userId: 99);
		Seed(h.Repository, authorId: 42);
		h.GuildClient.Result = new ChannelMembership(IsMember: false, GuildId: 5, Permissions: 0);

		var result = await handler.HandleAsync(new DeleteMessageCommand(MessageId: 1));

		Assert.True(result.IsFailure);
		Assert.Equal(MessageFailures.NotFound, result.Error);
		Assert.Empty(h.Repository.SoftDeleted);
	}

	[Fact]
	public async Task NonAuthor_ChannelNotFound_ReturnsNotFound()
	{
		var (h, handler) = BuildHandler(userId: 99);
		Seed(h.Repository, authorId: 42);
		h.GuildClient.Result = null;

		var result = await handler.HandleAsync(new DeleteMessageCommand(MessageId: 1));

		Assert.True(result.IsFailure);
		Assert.Equal(MessageFailures.NotFound, result.Error);
	}

	[Fact]
	public async Task NonAuthor_MemberWithoutManageMessages_ReturnsMissingManagePermission()
	{
		var (h, handler) = BuildHandler(userId: 99);
		Seed(h.Repository, authorId: 42);
		h.GuildClient.Result = new ChannelMembership(IsMember: true, GuildId: 5, Permissions: 0);

		var result = await handler.HandleAsync(new DeleteMessageCommand(MessageId: 1));

		Assert.True(result.IsFailure);
		Assert.Equal(MessageFailures.MissingManagePermission, result.Error);
		Assert.Empty(h.Repository.SoftDeleted);
	}

	[Fact]
	public async Task NonAuthor_WithManageMessages_DeletesAndBroadcasts()
	{
		var (h, handler) = BuildHandler(userId: 99);
		Seed(h.Repository, id: 1, channelId: 100, authorId: 42);
		h.GuildClient.Result = new ChannelMembership(IsMember: true, GuildId: 5, Permissions: ManageMessagesPermission);

		var result = await handler.HandleAsync(new DeleteMessageCommand(MessageId: 1));

		Assert.True(result.Succeeded);
		Assert.Single(h.Repository.SoftDeleted);

		var (broadcastChannel, broadcastMessageId) = Assert.Single(h.Broadcaster.DeletedBroadcasts);
		Assert.Equal(100L, broadcastChannel);
		Assert.Equal(1L, broadcastMessageId);
	}

	[Fact]
	public async Task NonAuthor_Administrator_DeletesAndBroadcasts()
	{
		var (h, handler) = BuildHandler(userId: 99);
		Seed(h.Repository, authorId: 42);
		h.GuildClient.Result = new ChannelMembership(IsMember: true, GuildId: 5, Permissions: AdministratorPermission);

		var result = await handler.HandleAsync(new DeleteMessageCommand(MessageId: 1));

		Assert.True(result.Succeeded);
		Assert.Single(h.Repository.SoftDeleted);
		Assert.Single(h.Broadcaster.DeletedBroadcasts);
	}
}
