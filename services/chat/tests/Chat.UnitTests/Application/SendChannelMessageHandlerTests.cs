using Chat.Application.Abstractions;
using Chat.Application.Contracts;
using Chat.Application.Features.Channels.Common;
using Chat.Application.Features.Channels.SendMessage;
using Chat.Domain.Attachments;
using Chat.Domain.Messages;
using Chat.Domain.Results;
using Chat.UnitTests.Fakes;
using Xunit;

namespace Chat.UnitTests.Application;

public sealed class SendChannelMessageHandlerTests
{
	// matches the constants in SendChannelMessageHandler
	private const long SendMessagesPermission = 1L << 0;
	private const long AdministratorPermission = 1L << 8;

	private sealed record Harness(
		FakeCurrentUser CurrentUser,
		FakeGuildClient GuildClient,
		FakeMessageRepository Repository,
		FakeAttachmentRepository AttachmentRepository,
		FakeIdGenerator IdGenerator,
		FakeClock Clock,
		FakeEventBus EventBus,
		FakeChannelBroadcaster Broadcaster);

	private static (Harness Harness,
		Chat.Application.Abstractions.Messaging.ICommandHandler<SendChannelMessageCommand, Result<ChannelMessageResponse>> Handler)
		BuildHandler(long userId = 42)
	{
		var currentUser = new FakeCurrentUser { UserId = userId };
		var guildClient = new FakeGuildClient();
		var repository = new FakeMessageRepository();
		var attachmentRepository = new FakeAttachmentRepository();
		var ids = new FakeIdGenerator();
		var clock = new FakeClock();
		var eventBus = new FakeEventBus();
		var broadcaster = new FakeChannelBroadcaster();

		var handler = HandlerFactory.CreateCommand<SendChannelMessageCommand, Result<ChannelMessageResponse>>(
			currentUser, repository, attachmentRepository, ids, clock, guildClient, eventBus, broadcaster);

		return (new Harness(currentUser, guildClient, repository, attachmentRepository, ids, clock, eventBus, broadcaster), handler);
	}

	[Fact]
	public async Task ChannelNotFound_ReturnsChannelNotFound_NoSideEffects()
	{
		var (h, handler) = BuildHandler();
		h.GuildClient.Result = null;

		var result = await handler.HandleAsync(new SendChannelMessageCommand(ChannelId: 100, Content: "hi", ReplyToId: null, AttachmentIds: [], Nonce: null));

		Assert.True(result.IsFailure);
		Assert.Equal(MessageFailures.ChannelNotFound, result.Error);
		Assert.Empty(h.Repository.Saved);
		Assert.Empty(h.EventBus.Published);
		Assert.Empty(h.Broadcaster.Broadcasts);
	}

	[Fact]
	public async Task NonMember_ReturnsNotAMember_NoSideEffects()
	{
		var (h, handler) = BuildHandler();
		h.GuildClient.Result = new ChannelMembership(IsMember: false, GuildId: 5, Permissions: 0);

		var result = await handler.HandleAsync(new SendChannelMessageCommand(ChannelId: 100, Content: "hi", ReplyToId: null, AttachmentIds: [], Nonce: null));

		Assert.True(result.IsFailure);
		Assert.Equal(MessageFailures.NotAMember, result.Error);
		Assert.Empty(h.Repository.Saved);
	}

	[Fact]
	public async Task MissingSendPermission_ReturnsMissingSendPermission_NoSideEffects()
	{
		var (h, handler) = BuildHandler();
		h.GuildClient.Result = new ChannelMembership(IsMember: true, GuildId: 5, Permissions: 0);

		var result = await handler.HandleAsync(new SendChannelMessageCommand(ChannelId: 100, Content: "hi", ReplyToId: null, AttachmentIds: [], Nonce: null));

		Assert.True(result.IsFailure);
		Assert.Equal(MessageFailures.MissingSendPermission, result.Error);
		Assert.Empty(h.Repository.Saved);
		Assert.Empty(h.EventBus.Published);
		Assert.Empty(h.Broadcaster.Broadcasts);
	}

	[Fact]
	public async Task AdministratorBypassesSendMessages_HappyPath()
	{
		var (h, handler) = BuildHandler();
		// admin bit set, SEND_MESSAGES clear: should still go through
		h.GuildClient.Result = new ChannelMembership(IsMember: true, GuildId: 5, Permissions: AdministratorPermission);

		var result = await handler.HandleAsync(new SendChannelMessageCommand(ChannelId: 100, Content: "hi", ReplyToId: null, AttachmentIds: [], Nonce: null));

		Assert.True(result.Succeeded);
		Assert.Single(h.Repository.Saved);
		Assert.Single(h.EventBus.Published);
		Assert.Single(h.Broadcaster.Broadcasts);
	}

	[Fact]
	public async Task HappyPath_PopulatesResponse_PersistsMessage_PublishesEvent_Broadcasts()
	{
		var (h, handler) = BuildHandler(userId: 42);
		h.GuildClient.Result = new ChannelMembership(IsMember: true, GuildId: 9, Permissions: SendMessagesPermission);
		SeedReplyTarget(h.Repository, id: 7, channelId: 100);

		var result = await handler.HandleAsync(new SendChannelMessageCommand(ChannelId: 100, Content: "hello", ReplyToId: 7, AttachmentIds: [], Nonce: null));

		Assert.True(result.Succeeded);
		var response = result.Value;

		Assert.Equal("100", response.ChannelId);
		Assert.Equal("42", response.AuthorId);
		Assert.Equal("hello", response.Content);
		Assert.Equal("7", response.ReplyToId);
		Assert.True(long.TryParse(response.Id, out var responseId));
		Assert.Empty(response.Attachments);
		Assert.Empty(response.Reactions);
		Assert.Null(response.EditedAt);

		// repo persisted the new message (alongside the seeded reply target)
		var saved = Assert.Single(h.Repository.Saved, m => m.Id == responseId);
		Assert.Equal(100L, saved.ContainerId);
		Assert.Equal(42L, saved.AuthorId);

		// event published with the right MessageId / GuildId / AuthorId
		var evt = Assert.Single(h.EventBus.PublishedOf<ChatMessageSent>());
		Assert.Equal(100L, evt.ChannelId);
		Assert.Equal(9L, evt.GuildId);
		Assert.Equal(42L, evt.AuthorId);
		Assert.Equal(responseId, evt.MessageId);
		Assert.Equal("hello", evt.Content);
		Assert.Empty(evt.Mentions);

		// broadcast went out exactly once
		var (broadcastChannel, broadcastMessage) = Assert.Single(h.Broadcaster.Broadcasts);
		Assert.Equal(100L, broadcastChannel);
		Assert.Same(response, broadcastMessage);
	}

	[Fact]
	public async Task Nonce64Chars_MaxAllowed_Succeeds()
	{
		var (h, handler) = BuildHandler();
		h.GuildClient.Result = new ChannelMembership(IsMember: true, GuildId: 5, Permissions: SendMessagesPermission);
		var nonce = new string('x', 64);

		var result = await handler.HandleAsync(new SendChannelMessageCommand(ChannelId: 100, Content: "hi", ReplyToId: null, AttachmentIds: [], Nonce: nonce));

		Assert.True(result.Succeeded);
		Assert.Equal(nonce, result.Value.Nonce);
	}

	[Fact]
	public async Task Nonce65Chars_OneTooLong_ReturnsNonceTooLong_NoSideEffects()
	{
		var (h, handler) = BuildHandler();
		h.GuildClient.Result = new ChannelMembership(IsMember: true, GuildId: 5, Permissions: SendMessagesPermission);

		var result = await handler.HandleAsync(new SendChannelMessageCommand(ChannelId: 100, Content: "hi", ReplyToId: null, AttachmentIds: [], Nonce: new string('x', 65)));

		Assert.True(result.IsFailure);
		Assert.Equal(MessageFailures.NonceTooLong, result.Error);
		Assert.Empty(h.Repository.Saved);
		Assert.Empty(h.EventBus.Published);
		Assert.Empty(h.Broadcaster.Broadcasts);
	}

	[Fact]
	public async Task NonceDedupHit_ReturnsSameMessage_NoSideEffects()
	{
		var (h, handler) = BuildHandler(userId: 42);
		h.GuildClient.Result = new ChannelMembership(IsMember: true, GuildId: 9, Permissions: SendMessagesPermission);

		// first call — persists the message
		var first = await handler.HandleAsync(new SendChannelMessageCommand(ChannelId: 100, Content: "hello", ReplyToId: null, AttachmentIds: [], Nonce: "my-nonce"));
		Assert.True(first.Succeeded);

		h.EventBus.Reset();
		h.Broadcaster.Reset();

		// second call with the same nonce — must return the original message
		var second = await handler.HandleAsync(new SendChannelMessageCommand(ChannelId: 100, Content: "hello", ReplyToId: null, AttachmentIds: [], Nonce: "my-nonce"));

		Assert.True(second.Succeeded);
		Assert.Equal(first.Value.Id, second.Value.Id);
		Assert.Equal(first.Value.CreatedAt, second.Value.CreatedAt);
		Assert.Equal("my-nonce", second.Value.Nonce);

		// no new persist, event, or broadcast on the retry
		Assert.Single(h.Repository.Saved);
		Assert.Empty(h.EventBus.Published);
		Assert.Empty(h.Broadcaster.Broadcasts);
	}

	[Fact]
	public async Task NonceDedupMiss_DifferentNonce_CreatesTwoMessages()
	{
		var (h, handler) = BuildHandler(userId: 42);
		h.GuildClient.Result = new ChannelMembership(IsMember: true, GuildId: 9, Permissions: SendMessagesPermission);

		var first = await handler.HandleAsync(new SendChannelMessageCommand(ChannelId: 100, Content: "hello", ReplyToId: null, AttachmentIds: [], Nonce: "nonce-a"));
		var second = await handler.HandleAsync(new SendChannelMessageCommand(ChannelId: 100, Content: "hello", ReplyToId: null, AttachmentIds: [], Nonce: "nonce-b"));

		Assert.True(first.Succeeded);
		Assert.True(second.Succeeded);
		Assert.NotEqual(first.Value.Id, second.Value.Id);
		Assert.Equal(2, h.Repository.Saved.Count);
		Assert.Equal(2, h.EventBus.Published.Count);
		Assert.Equal(2, h.Broadcaster.Broadcasts.Count);
	}

	[Fact]
	public async Task ContentRequired_DomainFailureBubblesUp_NoSideEffects()
	{
		var (h, handler) = BuildHandler();
		h.GuildClient.Result = new ChannelMembership(IsMember: true, GuildId: 5, Permissions: SendMessagesPermission);

		var result = await handler.HandleAsync(new SendChannelMessageCommand(ChannelId: 100, Content: "   ", ReplyToId: null, AttachmentIds: [], Nonce: null));

		Assert.True(result.IsFailure);
		Assert.Equal(MessageFailures.ContentRequired, result.Error);
		Assert.Empty(h.Repository.Saved);
		Assert.Empty(h.EventBus.Published);
		Assert.Empty(h.Broadcaster.Broadcasts);
	}

	private static void SeedReplyTarget(FakeMessageRepository repo, long id, long channelId, bool isDeleted = false)
	{
		var message = Message.Reconstitute(
			id: id, containerId: channelId, authorId: 1, recipientId: null, content: "original",
			replyToId: null, editedAt: null, isDeleted: isDeleted,
			createdAt: new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero));
		repo.Seed(message);
	}

	[Fact]
	public async Task ReplyToNonexistentMessage_ReturnsInvalidReplyTarget_NoSideEffects()
	{
		var (h, handler) = BuildHandler();
		h.GuildClient.Result = new ChannelMembership(IsMember: true, GuildId: 5, Permissions: SendMessagesPermission);

		var result = await handler.HandleAsync(new SendChannelMessageCommand(ChannelId: 100, Content: "hi", ReplyToId: 999, AttachmentIds: [], Nonce: null));

		Assert.True(result.IsFailure);
		Assert.Equal(MessageFailures.InvalidReplyTarget, result.Error);
		Assert.Empty(h.Repository.Saved);
		Assert.Empty(h.EventBus.Published);
		Assert.Empty(h.Broadcaster.Broadcasts);
	}

	[Fact]
	public async Task ReplyToDeletedMessage_ReturnsInvalidReplyTarget_NoSideEffects()
	{
		var (h, handler) = BuildHandler();
		h.GuildClient.Result = new ChannelMembership(IsMember: true, GuildId: 5, Permissions: SendMessagesPermission);
		SeedReplyTarget(h.Repository, id: 7, channelId: 100, isDeleted: true);

		var result = await handler.HandleAsync(new SendChannelMessageCommand(ChannelId: 100, Content: "hi", ReplyToId: 7, AttachmentIds: [], Nonce: null));

		Assert.True(result.IsFailure);
		Assert.Equal(MessageFailures.InvalidReplyTarget, result.Error);
	}

	[Fact]
	public async Task ReplyToMessageInDifferentChannel_ReturnsInvalidReplyTarget_NoSideEffects()
	{
		var (h, handler) = BuildHandler();
		h.GuildClient.Result = new ChannelMembership(IsMember: true, GuildId: 5, Permissions: SendMessagesPermission);
		SeedReplyTarget(h.Repository, id: 7, channelId: 200); // different channel than command targets

		var result = await handler.HandleAsync(new SendChannelMessageCommand(ChannelId: 100, Content: "hi", ReplyToId: 7, AttachmentIds: [], Nonce: null));

		Assert.True(result.IsFailure);
		Assert.Equal(MessageFailures.InvalidReplyTarget, result.Error);
	}

	[Fact]
	public async Task ReplyToValidMessage_Succeeds()
	{
		var (h, handler) = BuildHandler();
		h.GuildClient.Result = new ChannelMembership(IsMember: true, GuildId: 5, Permissions: SendMessagesPermission);
		SeedReplyTarget(h.Repository, id: 7, channelId: 100);

		var result = await handler.HandleAsync(new SendChannelMessageCommand(ChannelId: 100, Content: "hi", ReplyToId: 7, AttachmentIds: [], Nonce: null));

		Assert.True(result.Succeeded);
		Assert.Equal("7", result.Value.ReplyToId);
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
		h.GuildClient.Result = new ChannelMembership(IsMember: true, GuildId: 9, Permissions: SendMessagesPermission);
		var draft = SeedDraft(h.AttachmentRepository, id: 555, uploaderId: 42);

		var result = await handler.HandleAsync(new SendChannelMessageCommand(
			ChannelId: 100, Content: "look", ReplyToId: null, AttachmentIds: [draft.Id], Nonce: null));

		Assert.True(result.Succeeded);
		var attachment = Assert.Single(result.Value.Attachments);
		Assert.Equal("555", attachment.Id);
		Assert.Equal("pic.png", attachment.Filename);

		// the message persisted with its attachment metadata attached
		var saved = Assert.Single(h.Repository.Saved);
		var persisted = Assert.Single(h.Repository.SavedAttachments[saved.Id]);
		Assert.Equal(555L, persisted.Id);
	}

	[Fact]
	public async Task AttachmentWithoutContent_Succeeds()
	{
		var (h, handler) = BuildHandler(userId: 42);
		h.GuildClient.Result = new ChannelMembership(IsMember: true, GuildId: 9, Permissions: SendMessagesPermission);
		var draft = SeedDraft(h.AttachmentRepository, id: 777, uploaderId: 42);

		var result = await handler.HandleAsync(new SendChannelMessageCommand(
			ChannelId: 100, Content: null, ReplyToId: null, AttachmentIds: [draft.Id], Nonce: null));

		Assert.True(result.Succeeded);
		Assert.Single(result.Value.Attachments);
	}

	[Fact]
	public async Task TooManyAttachments_ReturnsTooMany_NoSideEffects()
	{
		var (h, handler) = BuildHandler(userId: 42);
		h.GuildClient.Result = new ChannelMembership(IsMember: true, GuildId: 9, Permissions: SendMessagesPermission);
		long[] ids = [.. Enumerable.Range(1, 11).Select(i => (long)i)];

		var result = await handler.HandleAsync(new SendChannelMessageCommand(
			ChannelId: 100, Content: "hi", ReplyToId: null, AttachmentIds: ids, Nonce: null));

		Assert.True(result.IsFailure);
		Assert.Equal(AttachmentFailures.TooMany, result.Error);
		Assert.Empty(h.Repository.Saved);
		Assert.Empty(h.EventBus.Published);
		Assert.Empty(h.Broadcaster.Broadcasts);
	}

	[Fact]
	public async Task UnknownDraft_ReturnsInvalidReference_NoSideEffects()
	{
		var (h, handler) = BuildHandler(userId: 42);
		h.GuildClient.Result = new ChannelMembership(IsMember: true, GuildId: 9, Permissions: SendMessagesPermission);

		var result = await handler.HandleAsync(new SendChannelMessageCommand(
			ChannelId: 100, Content: "hi", ReplyToId: null, AttachmentIds: [999], Nonce: null));

		Assert.True(result.IsFailure);
		Assert.Equal(AttachmentFailures.InvalidReference, result.Error);
		Assert.Empty(h.Repository.Saved);
	}

	[Fact]
	public async Task DraftOwnedByAnotherUser_ReturnsInvalidReference()
	{
		var (h, handler) = BuildHandler(userId: 42);
		h.GuildClient.Result = new ChannelMembership(IsMember: true, GuildId: 9, Permissions: SendMessagesPermission);
		var draft = SeedDraft(h.AttachmentRepository, id: 555, uploaderId: 7); // not 42

		var result = await handler.HandleAsync(new SendChannelMessageCommand(
			ChannelId: 100, Content: "hi", ReplyToId: null, AttachmentIds: [draft.Id], Nonce: null));

		Assert.True(result.IsFailure);
		Assert.Equal(AttachmentFailures.InvalidReference, result.Error);
		Assert.Empty(h.Repository.Saved);
	}

	[Fact]
	public async Task AlreadyAttachedDraft_ReturnsInvalidReference()
	{
		var (h, handler) = BuildHandler(userId: 42);
		h.GuildClient.Result = new ChannelMembership(IsMember: true, GuildId: 9, Permissions: SendMessagesPermission);
		var draft = SeedDraft(h.AttachmentRepository, id: 555, uploaderId: 42);
		h.AttachmentRepository.MarkAttached(draft.Id);

		var result = await handler.HandleAsync(new SendChannelMessageCommand(
			ChannelId: 100, Content: "hi", ReplyToId: null, AttachmentIds: [draft.Id], Nonce: null));

		Assert.True(result.IsFailure);
		Assert.Equal(AttachmentFailures.InvalidReference, result.Error);
		Assert.Empty(h.Repository.Saved);
	}
}
