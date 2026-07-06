using Chat.Application.Abstractions;
using Chat.Application.Abstractions.Messaging;
using Chat.Application.Features.Channels.Common;
using Chat.Application.Features.Reactions.AddReaction;
using Chat.Domain.Messages;
using Chat.Domain.Results;
using Chat.UnitTests.Fakes;
using Xunit;

namespace Chat.UnitTests.Application;

public sealed class AddReactionHandlerTests
{
	private const long ReadMessagesPermission = 1L << 1;
	private const long AdministratorPermission = 1L << 8;

	private sealed record Harness(
		FakeCurrentUser CurrentUser,
		FakeMessageRepository Repository,
		FakeReactionRepository ReactionRepository,
		FakeGuildClient GuildClient,
		FakeClock Clock,
		FakeChannelBroadcaster Broadcaster);

	private static (Harness Harness, ICommandHandler<AddReactionCommand, Result<ReactionResponse>> Handler)
		BuildHandler(long userId = 42)
	{
		var currentUser = new FakeCurrentUser { UserId = userId };
		var repository = new FakeMessageRepository();
		var reactionRepository = new FakeReactionRepository();
		var guildClient = new FakeGuildClient();
		var clock = new FakeClock();
		var broadcaster = new FakeChannelBroadcaster();

		var handler = HandlerFactory.CreateCommand<AddReactionCommand, Result<ReactionResponse>>(
			currentUser, repository, reactionRepository, guildClient, clock, broadcaster);

		return (new Harness(currentUser, repository, reactionRepository, guildClient, clock, broadcaster), handler);
	}

	private static Message SeedChannelMessage(FakeMessageRepository repo, long id = 1, long channelId = 100, long authorId = 99, bool isDeleted = false)
	{
		var message = Message.Reconstitute(
			id: id, containerId: channelId, authorId: authorId, recipientId: null,
			content: "hello", replyToId: null, editedAt: null,
			isDeleted: isDeleted, createdAt: DateTimeOffset.UtcNow);
		repo.Seed(message);
		return message;
	}

	private static Message SeedDirectMessage(FakeMessageRepository repo, long id, long conversationId, long authorId, long recipientId)
	{
		var message = Message.Reconstitute(
			id: id, containerId: conversationId, authorId: authorId, recipientId: recipientId,
			content: "hello", replyToId: null, editedAt: null,
			isDeleted: false, createdAt: DateTimeOffset.UtcNow);
		repo.Seed(message);
		return message;
	}

	[Fact]
	public async Task EmptyEmoji_ReturnsEmojiRequired_NoSideEffects()
	{
		var (h, handler) = BuildHandler();
		SeedChannelMessage(h.Repository);

		var result = await handler.HandleAsync(new AddReactionCommand(MessageId: 1, Emoji: "   "));

		Assert.True(result.IsFailure);
		Assert.Equal(ReactionFailures.EmojiRequired, result.Error);
		Assert.Empty(h.Broadcaster.ReactionAddedBroadcasts);
	}

	[Fact]
	public async Task EmojiTooLong_ReturnsEmojiTooLong_NoSideEffects()
	{
		var (h, handler) = BuildHandler();
		SeedChannelMessage(h.Repository);

		var result = await handler.HandleAsync(new AddReactionCommand(MessageId: 1, Emoji: new string('x', 33)));

		Assert.True(result.IsFailure);
		Assert.Equal(ReactionFailures.EmojiTooLong, result.Error);
		Assert.Empty(h.Broadcaster.ReactionAddedBroadcasts);
	}

	[Fact]
	public async Task MessageNotFound_ReturnsNotFound_NoSideEffects()
	{
		var (h, handler) = BuildHandler();

		var result = await handler.HandleAsync(new AddReactionCommand(MessageId: 999, Emoji: "👍"));

		Assert.True(result.IsFailure);
		Assert.Equal(MessageFailures.NotFound, result.Error);
		Assert.Empty(h.Broadcaster.ReactionAddedBroadcasts);
	}

	[Fact]
	public async Task DeletedMessage_ReturnsNotFound_NoSideEffects()
	{
		var (h, handler) = BuildHandler();
		SeedChannelMessage(h.Repository, isDeleted: true);

		var result = await handler.HandleAsync(new AddReactionCommand(MessageId: 1, Emoji: "👍"));

		Assert.True(result.IsFailure);
		Assert.Equal(MessageFailures.NotFound, result.Error);
	}

	[Fact]
	public async Task DirectMessage_ReturnsIsDirectMessage_NoSideEffects()
	{
		var (h, handler) = BuildHandler(userId: 42);
		SeedDirectMessage(h.Repository, id: 1, conversationId: 555, authorId: 42, recipientId: 100);

		var result = await handler.HandleAsync(new AddReactionCommand(MessageId: 1, Emoji: "👍"));

		Assert.True(result.IsFailure);
		Assert.Equal(ReactionFailures.IsDirectMessage, result.Error);
		Assert.Empty(h.Broadcaster.ReactionAddedBroadcasts);
	}

	[Fact]
	public async Task NotAMember_ReturnsMissingReadPermission_NoSideEffects()
	{
		var (h, handler) = BuildHandler();
		SeedChannelMessage(h.Repository);
		h.GuildClient.Result = new ChannelMembership(IsMember: false, GuildId: 5, Permissions: ReadMessagesPermission);

		var result = await handler.HandleAsync(new AddReactionCommand(MessageId: 1, Emoji: "👍"));

		Assert.True(result.IsFailure);
		Assert.Equal(MessageFailures.MissingReadPermission, result.Error);
	}

	[Fact]
	public async Task MemberWithoutReadPermission_ReturnsMissingReadPermission_NoSideEffects()
	{
		var (h, handler) = BuildHandler();
		SeedChannelMessage(h.Repository);
		h.GuildClient.Result = new ChannelMembership(IsMember: true, GuildId: 5, Permissions: 0);

		var result = await handler.HandleAsync(new AddReactionCommand(MessageId: 1, Emoji: "👍"));

		Assert.True(result.IsFailure);
		Assert.Equal(MessageFailures.MissingReadPermission, result.Error);
	}

	[Fact]
	public async Task Administrator_BypassesReadPermissionCheck()
	{
		var (h, handler) = BuildHandler(userId: 42);
		SeedChannelMessage(h.Repository, id: 1, channelId: 100);
		h.GuildClient.Result = new ChannelMembership(IsMember: true, GuildId: 5, Permissions: AdministratorPermission);

		var result = await handler.HandleAsync(new AddReactionCommand(MessageId: 1, Emoji: "👍"));

		Assert.True(result.Succeeded);
	}

	[Fact]
	public async Task HappyPath_AddsReaction_Broadcasts_ReturnsMeReactedTrue()
	{
		var (h, handler) = BuildHandler(userId: 42);
		SeedChannelMessage(h.Repository, id: 1, channelId: 100);
		h.GuildClient.Result = new ChannelMembership(IsMember: true, GuildId: 5, Permissions: ReadMessagesPermission);

		var result = await handler.HandleAsync(new AddReactionCommand(MessageId: 1, Emoji: "👍"));

		Assert.True(result.Succeeded);
		Assert.Equal("👍", result.Value.Emoji);
		Assert.Equal(1, result.Value.Count);
		Assert.True(result.Value.MeReacted);

		var (channelId, evt) = Assert.Single(h.Broadcaster.ReactionAddedBroadcasts);
		Assert.Equal(100L, channelId);
		Assert.Equal("1", evt.MessageId);
		Assert.Equal("100", evt.ChannelId);
		Assert.Equal("42", evt.UserId);
		Assert.Equal("👍", evt.Emoji);
		Assert.Equal(1, evt.Count);
	}

	[Fact]
	public async Task AlreadyReacted_IsIdempotent_NoBroadcast_ReturnsCurrentCount()
	{
		var (h, handler) = BuildHandler(userId: 42);
		SeedChannelMessage(h.Repository, id: 1, channelId: 100);
		h.GuildClient.Result = new ChannelMembership(IsMember: true, GuildId: 5, Permissions: ReadMessagesPermission);
		h.ReactionRepository.Seed(channelId: 100, messageId: 1, emoji: "👍", userId: 42);

		var result = await handler.HandleAsync(new AddReactionCommand(MessageId: 1, Emoji: "👍"));

		Assert.True(result.Succeeded);
		Assert.Equal(1, result.Value.Count);
		Assert.True(result.Value.MeReacted);
		Assert.Empty(h.Broadcaster.ReactionAddedBroadcasts);
	}
}
