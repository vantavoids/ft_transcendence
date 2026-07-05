using Chat.Application.Abstractions.Messaging;
using Chat.Application.Features.DirectMessages.Common;
using Chat.Application.Features.DirectMessages.UpdateReadState;
using Chat.Domain.Messages;
using Chat.Domain.Results;
using Chat.UnitTests.Fakes;
using Xunit;

namespace Chat.UnitTests.Application;

public sealed class UpdateDmReadStateHandlerTests
{
	private sealed record Harness(
		FakeCurrentUser CurrentUser,
		FakeMessageRepository Repository,
		FakeReadStateRepository ReadStates,
		FakeUserBroadcaster Broadcaster);

	private static (Harness Harness, ICommandHandler<UpdateDmReadStateCommand, Result<DmReadStateResponse>> Handler)
		BuildHandler(long userId = 42)
	{
		var currentUser = new FakeCurrentUser { UserId = userId };
		var repository = new FakeMessageRepository();
		var readStates = new FakeReadStateRepository();
		var broadcaster = new FakeUserBroadcaster();

		var handler = HandlerFactory.CreateCommand<UpdateDmReadStateCommand, Result<DmReadStateResponse>>(
			currentUser, repository, readStates, broadcaster);

		return (new Harness(currentUser, repository, readStates, broadcaster), handler);
	}

	[Fact]
	public async Task NoConversation_ReturnsConversationNotFound()
	{
		var (_, handler) = BuildHandler(userId: 42);

		var result = await handler.HandleAsync(new UpdateDmReadStateCommand(PartnerId: 100, MessageId: 1));

		Assert.True(result.IsFailure);
		Assert.Equal(MessageFailures.ConversationNotFound, result.Error);
	}

	[Fact]
	public async Task UnknownMessage_ReturnsNotFound()
	{
		var (h, handler) = BuildHandler(userId: 42);
		h.Repository.WithConversation(42, 100, conversationId: 555);

		var result = await handler.HandleAsync(new UpdateDmReadStateCommand(PartnerId: 100, MessageId: 999));

		Assert.True(result.IsFailure);
		Assert.Equal(MessageFailures.NotFound, result.Error);
	}

	[Fact]
	public async Task MessageInAnotherConversation_ReturnsNotFound()
	{
		var (h, handler) = BuildHandler(userId: 42);
		h.Repository.WithConversation(42, 100, conversationId: 555);
		h.Repository.Seed(Message.Reconstitute(
			id: 1, containerId: 999, authorId: 100, recipientId: 7,
			content: "hi", replyToId: null, editedAt: null, isDeleted: false,
			createdAt: DateTimeOffset.UtcNow));

		var result = await handler.HandleAsync(new UpdateDmReadStateCommand(PartnerId: 100, MessageId: 1));

		Assert.True(result.IsFailure);
		Assert.Equal(MessageFailures.NotFound, result.Error);
	}

	[Fact]
	public async Task HappyPath_AdvancesCursor_ResetsUnread_ReturnsZero()
	{
		var (h, handler) = BuildHandler(userId: 42);
		h.Repository.WithConversation(42, 100, conversationId: 555);
		h.Repository.Seed(Message.Reconstitute(
			id: 1, containerId: 555, authorId: 100, recipientId: 42,
			content: "hi", replyToId: null, editedAt: null, isDeleted: false,
			createdAt: new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero)));

		var result = await handler.HandleAsync(new UpdateDmReadStateCommand(PartnerId: 100, MessageId: 1));

		Assert.True(result.Succeeded);
		Assert.Equal("100", result.Value.PartnerId);
		Assert.Equal("1", result.Value.LastReadMessageId);
		Assert.Equal(0, result.Value.UnreadCount);
		Assert.Single(h.ReadStates.ResetDmUnreadCounts, (42L, 100L));

		var (broadcastUserId, broadcastResponse) = Assert.Single(h.Broadcaster.DmReadStateCalls);
		Assert.Equal(42L, broadcastUserId);
		Assert.Same(result.Value, broadcastResponse);
	}

	[Fact]
	public async Task OlderMessageId_IsNoOp_KeepsNewerCursor_StillResetsUnread()
	{
		var (h, handler) = BuildHandler(userId: 42);
		h.Repository.WithConversation(42, 100, conversationId: 555);
		var anchor = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
		h.Repository.Seed(Message.Reconstitute(
			id: 1, containerId: 555, authorId: 100, recipientId: 42,
			content: "hi", replyToId: null, editedAt: null, isDeleted: false, createdAt: anchor));
		h.Repository.Seed(Message.Reconstitute(
			id: 2, containerId: 555, authorId: 100, recipientId: 42,
			content: "hi2", replyToId: null, editedAt: null, isDeleted: false, createdAt: anchor.AddMinutes(1)));

		var first = await handler.HandleAsync(new UpdateDmReadStateCommand(PartnerId: 100, MessageId: 2));
		Assert.True(first.Succeeded);
		Assert.Equal("2", first.Value.LastReadMessageId);

		var second = await handler.HandleAsync(new UpdateDmReadStateCommand(PartnerId: 100, MessageId: 1));
		Assert.True(second.Succeeded);
		Assert.Equal("2", second.Value.LastReadMessageId); // unchanged, message 1 is older
		Assert.Equal(2, h.ReadStates.ResetDmUnreadCounts.Count); // still resets on every successful call
	}
}
