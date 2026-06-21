using Chat.Application.Abstractions.Messaging;
using Chat.Application.Contracts;
using Chat.Application.Features.DirectMessages.Common;
using Chat.Application.Features.DirectMessages.SendMessage;
using Chat.Domain.Results;
using Chat.UnitTests.Fakes;
using Xunit;

namespace Chat.UnitTests.Application;

public sealed class SendDirectMessageHandlerTests
{
	private sealed record Harness(
		FakeCurrentUser CurrentUser,
		FakeDirectMessageRepository Repository,
		FakeIdGenerator IdGenerator,
		FakeClock Clock,
		FakeEventBus EventBus,
		FakeConversationUnicast Unicaster);

	private static (Harness Harness,
		ICommandHandler<SendDirectMessageCommand, Result<DirectMessageResponse>> Handler)
		BuildHandler(long userId = 42)
	{
		var currentUser = new FakeCurrentUser { UserId = userId };
		var repository = new FakeDirectMessageRepository();
		var ids = new FakeIdGenerator();
		var clock = new FakeClock();
		var eventBus = new FakeEventBus();
		var unicaster = new FakeConversationUnicast();

		var handler = HandlerFactory.CreateCommand<SendDirectMessageCommand, Result<DirectMessageResponse>>(
			currentUser, repository, ids, clock, eventBus, unicaster);

		return (new Harness(currentUser, repository, ids, clock, eventBus, unicaster), handler);
	}

	[Fact]
	public async Task HappyPath_NewConversation_Persists_Publishes_Unicasts()
	{
		var (h, handler) = BuildHandler(userId: 42);

		var result = await handler.HandleAsync(
			new SendDirectMessageCommand(RecipientId: 100, Content: "hello", ReplyToId: null, Nonce: null));

		Assert.True(result.Succeeded);
		var response = result.Value;

		var saved = Assert.Single(h.Repository.Saved);
		Assert.Equal(42L, saved.SenderId);
		Assert.Equal(100L, saved.RecipientId);
		Assert.Equal("hello", saved.Content);
		Assert.True(saved.ConversationId > 0);

		Assert.True(long.TryParse(response.Id, out var responseId));
		Assert.Equal(responseId, saved.Id);

		var evt = Assert.Single(h.EventBus.PublishedOf<ChatDmSent>());
		Assert.Equal(42L, evt.SenderId);
		Assert.Equal(100L, evt.RecipientId);
		Assert.Equal(saved.Id, evt.MessageId);
		Assert.Equal(saved.ConversationId, evt.ConversationId);
		Assert.Equal("hello", evt.Content);

		var (recipientId, unicastMessage) = Assert.Single(h.Unicaster.Unicasts);
		Assert.Equal(100L, recipientId);
		Assert.Same(response, unicastMessage);
	}

	[Fact]
	public async Task ExistingConversation_ReusesId_DoesNotGenerateNew()
	{
		var (h, handler) = BuildHandler(userId: 42);
		h.Repository.WithConversation(42, 100, conversationId: 555);   // la paire a déjà une conversation

		var result = await handler.HandleAsync(
			new SendDirectMessageCommand(RecipientId: 100, Content: "hi", ReplyToId: null, Nonce: null));

		Assert.True(result.Succeeded);
		var saved = Assert.Single(h.Repository.Saved);
		Assert.Equal(555L, saved.ConversationId);                     // réutilisé, pas régénéré
		Assert.Equal(555L, Assert.Single(h.EventBus.PublishedOf<ChatDmSent>()).ConversationId);
	}

	[Fact]
	public async Task NonceTooLong_ReturnsFailure_NoSideEffects()
	{
		var (h, handler) = BuildHandler();

		var result = await handler.HandleAsync(
			new SendDirectMessageCommand(RecipientId: 100, Content: "hi", ReplyToId: null, Nonce: new string('x', 65)));

		Assert.True(result.IsFailure);
		Assert.Equal(DirectMessageFailures.NonceTooLong, result.Error);
		Assert.Empty(h.Repository.Saved);
		Assert.Empty(h.EventBus.Published);
		Assert.Empty(h.Unicaster.Unicasts);
	}

	[Fact]
	public async Task Nonce64Chars_MaxAllowed_Succeeds()
	{
		var (h, handler) = BuildHandler();
		var nonce = new string('x', 64);

		var result = await handler.HandleAsync(
			new SendDirectMessageCommand(RecipientId: 100, Content: "hi", ReplyToId: null, Nonce: nonce));

		Assert.True(result.Succeeded);
		Assert.Equal(nonce, result.Value.Nonce);   // ⚠️ aligne sur le nom de champ de ton DirectMessageResponse
	}

	[Fact]
	public async Task NonceDedupHit_ReturnsSameMessage_NoNewSideEffects()
	{
		var (h, handler) = BuildHandler(userId: 42);

		var first = await handler.HandleAsync(
			new SendDirectMessageCommand(RecipientId: 100, Content: "hello", ReplyToId: null, Nonce: "n1"));
		Assert.True(first.Succeeded);

		h.EventBus.Reset();
		h.Unicaster.Reset();

		var second = await handler.HandleAsync(
			new SendDirectMessageCommand(RecipientId: 100, Content: "hello", ReplyToId: null, Nonce: "n1"));

		Assert.True(second.Succeeded);
		Assert.Equal(first.Value.Id, second.Value.Id);
		Assert.Single(h.Repository.Saved);
		Assert.Empty(h.EventBus.Published);
		Assert.Empty(h.Unicaster.Unicasts);
	}

	[Fact]
	public async Task NonceDedupMiss_DifferentNonces_CreatesTwo()
	{
		var (h, handler) = BuildHandler(userId: 42);

		var first = await handler.HandleAsync(
			new SendDirectMessageCommand(RecipientId: 100, Content: "hello", ReplyToId: null, Nonce: "n-a"));
		var second = await handler.HandleAsync(
			new SendDirectMessageCommand(RecipientId: 100, Content: "hello", ReplyToId: null, Nonce: "n-b"));

		Assert.True(first.Succeeded);
		Assert.True(second.Succeeded);
		Assert.NotEqual(first.Value.Id, second.Value.Id);
		Assert.Equal(2, h.Repository.Saved.Count);
		Assert.Equal(2, h.EventBus.Published.Count);
		Assert.Equal(2, h.Unicaster.Unicasts.Count);
	}

	[Fact]
	public async Task ContentRequired_DomainFailureBubblesUp_NoSideEffects()
	{
		var (h, handler) = BuildHandler();

		var result = await handler.HandleAsync(
			new SendDirectMessageCommand(RecipientId: 100, Content: "   ", ReplyToId: null, Nonce: null));

		Assert.True(result.IsFailure);
		Assert.Equal(DirectMessageFailures.ContentRequired, result.Error);
		Assert.Empty(h.Repository.Saved);
		Assert.Empty(h.EventBus.Published);
		Assert.Empty(h.Unicaster.Unicasts);
	}

	[Fact]
	public async Task ReplyToId_NotInConversation_ReturnsInvalidReplyTarget_NoSideEffects()
	{
		var (h, handler) = BuildHandler(userId: 42);
		h.Repository.WithConversation(42, 100, conversationId: 555);

		var result = await handler.HandleAsync(
				new SendDirectMessageCommand(
					RecipientId: 100,
					Content: "reply",
					ReplyToId: 999,
					Nonce: null));

		Assert.True(result.IsFailure);
		Assert.Equal(DirectMessageFailures.InvalidReplyTarget, result.Error);
		Assert.Empty(h.Repository.Saved);
		Assert.Empty(h.EventBus.Published);
		Assert.Empty(h.Unicaster.Unicasts);
	}

	[Fact]
	public async Task ReplyToId_InSameConversation_Succeeds_AndPersistsReplyToId()
	{
		var (h, handler) = BuildHandler(userId: 42);
		h.Repository.WithConversation(42, 100, conversationId: 555);
		h.Repository.WithReply(conversationId: 555, replyToId: 999);

		var result = await handler.HandleAsync(
				new SendDirectMessageCommand(
					RecipientId: 100,
					Content: "reply",
					ReplyToId: 999,
					Nonce: null));

		Assert.True(result.Succeeded);

		var saved = Assert.Single(h.Repository.Saved);
		Assert.Equal(999L, saved.ReplyToId);
		Assert.Equal(555L, saved.ConversationId);
	}
}
