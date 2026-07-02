using Chat.Application.Abstractions;
using Chat.Application.Abstractions.Messaging;
using Chat.Application.Contracts;
using Chat.Application.Features.DirectMessages.Common;
using Chat.Application.Features.DirectMessages.SendMessage;
using Chat.Domain.Attachments;
using Chat.Domain.Messages;
using Chat.Domain.Results;
using Chat.UnitTests.Fakes;
using Xunit;

namespace Chat.UnitTests.Application;

public sealed class SendDirectMessageHandlerTests
{
	private sealed record Harness(
		FakeCurrentUser CurrentUser,
		FakeMessageRepository Repository,
		FakeAttachmentRepository AttachmentRepository,
		FakeIdGenerator IdGenerator,
		FakeUserClient UserClient,
		FakeClock Clock,
		FakeEventBus EventBus,
		FakeConversationUnicast Unicaster);

	private static (Harness Harness,
		ICommandHandler<SendDirectMessageCommand, Result<DirectMessageResponse>> Handler)
		BuildHandler(long userId = 42, long recipientId = 100)
	{
		var currentUser = new FakeCurrentUser { UserId = userId };
		var repository = new FakeMessageRepository();
		var attachmentRepository = new FakeAttachmentRepository();
		var ids = new FakeIdGenerator();
		var userClient = new FakeUserClient();
		userClient.Setup(userId, recipientId, new UsersRelationship("none", null));
		var clock = new FakeClock();
		var eventBus = new FakeEventBus();
		var unicaster = new FakeConversationUnicast();

		var handler = HandlerFactory.CreateCommand<SendDirectMessageCommand, Result<DirectMessageResponse>>(
			currentUser, repository, attachmentRepository, ids, clock, userClient, eventBus, unicaster);

		return (new Harness(currentUser, repository, attachmentRepository, ids, userClient, clock, eventBus, unicaster), handler);
	}

	[Fact]
	public async Task HappyPath_NewConversation_Persists_Publishes_Unicasts()
	{
		var (h, handler) = BuildHandler(userId: 42);

		var result = await handler.HandleAsync(
			new SendDirectMessageCommand(RecipientId: 100, Content: "hello", ReplyToId: null, AttachmentIds: [], Nonce: null));

		Assert.True(result.Succeeded);
		var response = result.Value;

		var saved = Assert.Single(h.Repository.Saved);
		Assert.Equal(42L, saved.AuthorId);
		Assert.Equal(100L, saved.RecipientId);
		Assert.Equal("hello", saved.Content);
		Assert.True(saved.ContainerId > 0);

		Assert.True(long.TryParse(response.Id, out var responseId));
		Assert.Equal(responseId, saved.Id);

		var evt = Assert.Single(h.EventBus.PublishedOf<ChatDmSent>());
		Assert.Equal(42L, evt.SenderId);
		Assert.Equal(100L, evt.RecipientId);
		Assert.Equal(saved.Id, evt.MessageId);
		Assert.Equal(saved.ContainerId, evt.ConversationId);
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
			new SendDirectMessageCommand(RecipientId: 100, Content: "hi", ReplyToId: null, AttachmentIds: [], Nonce: null));

		Assert.True(result.Succeeded);
		var saved = Assert.Single(h.Repository.Saved);
		Assert.Equal(555L, saved.ContainerId);                     // réutilisé, pas régénéré
		Assert.Equal(555L, Assert.Single(h.EventBus.PublishedOf<ChatDmSent>()).ConversationId);
	}

	[Fact]
	public async Task NonceTooLong_ReturnsFailure_NoSideEffects()
	{
		var (h, handler) = BuildHandler();

		var result = await handler.HandleAsync(
			new SendDirectMessageCommand(RecipientId: 100, Content: "hi", ReplyToId: null, AttachmentIds: [], Nonce: new string('x', 65)));

		Assert.True(result.IsFailure);
		Assert.Equal(MessageFailures.NonceTooLong, result.Error);
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
			new SendDirectMessageCommand(RecipientId: 100, Content: "hi", ReplyToId: null, AttachmentIds: [], Nonce: nonce));

		Assert.True(result.Succeeded);
		Assert.Equal(nonce, result.Value.Nonce);   // ⚠️ aligne sur le nom de champ de ton DirectMessageResponse
	}

	[Fact]
	public async Task NonceDedupHit_ReturnsSameMessage_NoNewSideEffects()
	{
		var (h, handler) = BuildHandler(userId: 42);

		var first = await handler.HandleAsync(
			new SendDirectMessageCommand(RecipientId: 100, Content: "hello", ReplyToId: null, AttachmentIds: [], Nonce: "n1"));
		Assert.True(first.Succeeded);

		h.EventBus.Reset();
		h.Unicaster.Reset();

		var second = await handler.HandleAsync(
			new SendDirectMessageCommand(RecipientId: 100, Content: "hello", ReplyToId: null, AttachmentIds: [], Nonce: "n1"));

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
			new SendDirectMessageCommand(RecipientId: 100, Content: "hello", ReplyToId: null, AttachmentIds: [], Nonce: "n-a"));
		var second = await handler.HandleAsync(
			new SendDirectMessageCommand(RecipientId: 100, Content: "hello", ReplyToId: null, AttachmentIds: [], Nonce: "n-b"));

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
			new SendDirectMessageCommand(RecipientId: 100, Content: "   ", ReplyToId: null, AttachmentIds: [], Nonce: null));

		Assert.True(result.IsFailure);
		Assert.Equal(MessageFailures.ContentRequired, result.Error);
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
					AttachmentIds: [],
					Nonce: null));

		Assert.True(result.IsFailure);
		Assert.Equal(MessageFailures.InvalidReplyTarget, result.Error);
		Assert.Empty(h.Repository.Saved);
		Assert.Empty(h.EventBus.Published);
		Assert.Empty(h.Unicaster.Unicasts);
	}

	[Fact]
	public async Task ReplyToId_InSameConversation_Succeeds_AndPersistsReplyToId()
	{
		var (h, handler) = BuildHandler(userId: 42);
		h.Repository.WithConversation(42, 100, conversationId: 555);
		h.Repository.Seed(Message.Reconstitute(
			id: 999, containerId: 555, authorId: 100, recipientId: 42,
			content: "original", replyToId: null, editedAt: null, isDeleted: false,
			createdAt: new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero)));

		var result = await handler.HandleAsync(
				new SendDirectMessageCommand(
					RecipientId: 100,
					Content: "reply",
					ReplyToId: 999,
					AttachmentIds: [],
					Nonce: null));

		Assert.True(result.Succeeded);

		var saved = Assert.Single(h.Repository.Saved, m => m.ReplyToId == 999L);
		Assert.Equal(555L, saved.ContainerId);
	}

	private static DraftAttachment SeedDraft(FakeAttachmentRepository repo, long id, long uploaderId)
	{
		var draft = DraftAttachment.Create(
			id: id, uploaderId: uploaderId,
			url: $"http://localhost/api/chat/v1/attachments/{id}/pic.png",
			filename: "pic.png", sizeBytes: 1024, mimeType: "image/png",
			now: new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero)).Value;
		repo.SeedDraft(draft);
		return draft;
	}

	[Fact]
	public async Task ValidDraft_AttachesToMessage_HydratesResponse_AndPersists()
	{
		var (h, handler) = BuildHandler(userId: 42);
		var draft = SeedDraft(h.AttachmentRepository, id: 555, uploaderId: 42);

		var result = await handler.HandleAsync(new SendDirectMessageCommand(
			RecipientId: 100, Content: "look", ReplyToId: null, AttachmentIds: [draft.Id], Nonce: null));

		Assert.True(result.Succeeded);
		var attachment = Assert.Single(result.Value.Attachments);
		Assert.Equal("555", attachment.Id);
		Assert.Equal("pic.png", attachment.Filename);

		var saved = Assert.Single(h.Repository.Saved);
		var persisted = Assert.Single(h.Repository.SavedAttachments[saved.Id]);
		Assert.Equal(555L, persisted.Id);
	}

	[Fact]
	public async Task AttachmentWithoutContent_Succeeds()
	{
		var (h, handler) = BuildHandler(userId: 42);
		var draft = SeedDraft(h.AttachmentRepository, id: 777, uploaderId: 42);

		var result = await handler.HandleAsync(new SendDirectMessageCommand(
			RecipientId: 100, Content: null, ReplyToId: null, AttachmentIds: [draft.Id], Nonce: null));

		Assert.True(result.Succeeded);
		Assert.Single(result.Value.Attachments);
	}

	[Fact]
	public async Task TooManyAttachments_ReturnsTooMany_NoSideEffects()
	{
		var (h, handler) = BuildHandler(userId: 42);
		long[] ids = [.. Enumerable.Range(1, 11).Select(i => (long)i)];

		var result = await handler.HandleAsync(new SendDirectMessageCommand(
			RecipientId: 100, Content: "hi", ReplyToId: null, AttachmentIds: ids, Nonce: null));

		Assert.True(result.IsFailure);
		Assert.Equal(AttachmentFailures.TooMany, result.Error);
		Assert.Empty(h.Repository.Saved);
		Assert.Empty(h.EventBus.Published);
		Assert.Empty(h.Unicaster.Unicasts);
	}

	[Fact]
	public async Task UnknownDraft_ReturnsInvalidReference_NoSideEffects()
	{
		var (h, handler) = BuildHandler(userId: 42);

		var result = await handler.HandleAsync(new SendDirectMessageCommand(
			RecipientId: 100, Content: "hi", ReplyToId: null, AttachmentIds: [999], Nonce: null));

		Assert.True(result.IsFailure);
		Assert.Equal(AttachmentFailures.InvalidReference, result.Error);
		Assert.Empty(h.Repository.Saved);
	}

	[Fact]
	public async Task DraftOwnedByAnotherUser_ReturnsInvalidReference()
	{
		var (h, handler) = BuildHandler(userId: 42);
		var draft = SeedDraft(h.AttachmentRepository, id: 555, uploaderId: 7); // not 42

		var result = await handler.HandleAsync(new SendDirectMessageCommand(
			RecipientId: 100, Content: "hi", ReplyToId: null, AttachmentIds: [draft.Id], Nonce: null));

		Assert.True(result.IsFailure);
		Assert.Equal(AttachmentFailures.InvalidReference, result.Error);
		Assert.Empty(h.Repository.Saved);
	}

	[Fact]
	public async Task AlreadyAttachedDraft_ReturnsInvalidReference()
	{
		var (h, handler) = BuildHandler(userId: 42);
		var draft = SeedDraft(h.AttachmentRepository, id: 555, uploaderId: 42);
		h.AttachmentRepository.MarkAttached(draft.Id);

		var result = await handler.HandleAsync(new SendDirectMessageCommand(
			RecipientId: 100, Content: "hi", ReplyToId: null, AttachmentIds: [draft.Id], Nonce: null));

		Assert.True(result.IsFailure);
		Assert.Equal(AttachmentFailures.InvalidReference, result.Error);
		Assert.Empty(h.Repository.Saved);
	}

	[Fact]
	public async Task NonceDedupHit_WithAttachments_RehydratesAttachmentsOnSecondCall()
	{
		var (h, handler) = BuildHandler(userId: 42);
		var draft = SeedDraft(h.AttachmentRepository, id: 555, uploaderId: 42);

		var first = await handler.HandleAsync(new SendDirectMessageCommand(
			RecipientId: 100, Content: "hello", ReplyToId: null, AttachmentIds: [draft.Id], Nonce: "n1"));
		Assert.True(first.Succeeded);
		Assert.Single(first.Value.Attachments);

		// the real repository writes dm_attachments + attachment_lookup atomically
		// with the message in the same batch; the fakes are decoupled, so mirror
		// that persisted state by hand before exercising the dedup rehydration path
		var saved = Assert.Single(h.Repository.Saved);
		h.AttachmentRepository.SeedDmAttachment(saved.ContainerId, saved.Id, draft.ToMetadata());

		var second = await handler.HandleAsync(new SendDirectMessageCommand(
			RecipientId: 100, Content: "hello", ReplyToId: null, AttachmentIds: [], Nonce: "n1"));

		Assert.True(second.Succeeded);
		Assert.Equal(first.Value.Id, second.Value.Id);
		var attachment = Assert.Single(second.Value.Attachments);
		Assert.Equal("555", attachment.Id);
		Assert.Single(h.Repository.Saved);
	}
}
