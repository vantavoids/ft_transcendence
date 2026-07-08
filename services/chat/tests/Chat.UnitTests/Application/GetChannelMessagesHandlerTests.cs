using Chat.Application.Abstractions;
using Chat.Application.Abstractions.Messaging;
using Chat.Application.Features.Channels.Common;
using Chat.Application.Features.Channels.GetChannelMessages;
using Chat.Domain.Attachments;
using Chat.Domain.Messages;
using Chat.Domain.Results;
using Chat.UnitTests.Fakes;
using Xunit;

namespace Chat.UnitTests.Application;

public sealed class GetChannelMessagesHandlerTests
{
	private const long ReadMessagesPermission = 1L << 1;
	private const long AdministratorPermission = 1L << 8;

	private sealed record Harness(
		FakeCurrentUser CurrentUser,
		FakeGuildClient GuildClient,
		FakeMessageRepository Repository,
		FakeAttachmentRepository AttachmentRepository,
		FakeReactionRepository ReactionRepository,
		FakeClock Clock);

	private static (Harness Harness, IQueryHandler<GetChannelMessagesQuery, Result<IReadOnlyList<ChannelMessageResponse>>> Handler)
		BuildHandler(long userId = 42)
	{
		var currentUser = new FakeCurrentUser { UserId = userId };
		var guildClient = new FakeGuildClient();
		var repository = new FakeMessageRepository();
		var attachmentRepository = new FakeAttachmentRepository();
		var reactionRepository = new FakeReactionRepository();
		var clock = new FakeClock();

		var handler = HandlerFactory.CreateQuery<GetChannelMessagesQuery, Result<IReadOnlyList<ChannelMessageResponse>>>(
			currentUser, guildClient, repository, attachmentRepository, reactionRepository, clock);

		return (new Harness(currentUser, guildClient, repository, attachmentRepository, reactionRepository, clock), handler);
	}

	private static Message SeedMessage(FakeMessageRepository repo, long id, long channelId, DateTimeOffset createdAt, bool isDeleted = false)
	{
		var message = Message.Reconstitute(
			id: id, containerId: channelId, authorId: 99, recipientId: null,
			content: "hello", replyToId: null, editedAt: null,
			isDeleted: isDeleted, createdAt: createdAt);
		repo.Seed(message);
		return message;
	}

	[Fact]
	public async Task ChannelNotFound_ReturnsChannelNotFound()
	{
		var (h, handler) = BuildHandler();
		h.GuildClient.Result = null;

		var result = await handler.HandleAsync(new GetChannelMessagesQuery(ChannelId: 100, BeforeTime: null, Limit: 50));

		Assert.True(result.IsFailure);
		Assert.Equal(MessageFailures.ChannelNotFound, result.Error);
	}

	[Fact]
	public async Task NotAMember_ReturnsNotAMember()
	{
		var (h, handler) = BuildHandler();
		h.GuildClient.Result = new ChannelMembership(IsMember: false, GuildId: 5, Permissions: ReadMessagesPermission);

		var result = await handler.HandleAsync(new GetChannelMessagesQuery(ChannelId: 100, BeforeTime: null, Limit: 50));

		Assert.True(result.IsFailure);
		Assert.Equal(MessageFailures.NotAMember, result.Error);
	}

	[Fact]
	public async Task MemberWithoutReadPermission_ReturnsMissingReadPermission()
	{
		var (h, handler) = BuildHandler();
		h.GuildClient.Result = new ChannelMembership(IsMember: true, GuildId: 5, Permissions: 0);

		var result = await handler.HandleAsync(new GetChannelMessagesQuery(ChannelId: 100, BeforeTime: null, Limit: 50));

		Assert.True(result.IsFailure);
		Assert.Equal(MessageFailures.MissingReadPermission, result.Error);
	}

	[Fact]
	public async Task HappyPath_ReturnsMessages_ExcludesDeleted()
	{
		var (h, handler) = BuildHandler();
		h.GuildClient.Result = new ChannelMembership(IsMember: true, GuildId: 5, Permissions: ReadMessagesPermission);

		var anchor = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
		SeedMessage(h.Repository, id: 1, channelId: 100, createdAt: anchor.AddMinutes(-10));
		SeedMessage(h.Repository, id: 2, channelId: 100, createdAt: anchor.AddMinutes(-5));
		SeedMessage(h.Repository, id: 3, channelId: 100, createdAt: anchor.AddMinutes(-2), isDeleted: true);

		var result = await handler.HandleAsync(new GetChannelMessagesQuery(ChannelId: 100, BeforeTime: anchor, Limit: 50));

		Assert.True(result.Succeeded);
		Assert.Equal(2, result.Value.Count);
		Assert.DoesNotContain(result.Value, m => m.Id == "3");
	}

	[Fact]
	public async Task NullBeforeTime_DefaultsToClockNow()
	{
		var (h, handler) = BuildHandler();
		h.GuildClient.Result = new ChannelMembership(IsMember: true, GuildId: 5, Permissions: ReadMessagesPermission);

		var now = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
		h.Clock.Set(now);
		SeedMessage(h.Repository, id: 1, channelId: 100, createdAt: now.AddMinutes(-1));

		var result = await handler.HandleAsync(new GetChannelMessagesQuery(ChannelId: 100, BeforeTime: null, Limit: 50));

		Assert.True(result.Succeeded);
		Assert.Single(result.Value);
	}

	[Fact]
	public async Task HydratesAttachments_PerMessage_InOneBatch()
	{
		var (h, handler) = BuildHandler();
		h.GuildClient.Result = new ChannelMembership(IsMember: true, GuildId: 5, Permissions: ReadMessagesPermission);

		var anchor = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
		SeedMessage(h.Repository, id: 1, channelId: 100, createdAt: anchor.AddMinutes(-10));
		SeedMessage(h.Repository, id: 2, channelId: 100, createdAt: anchor.AddMinutes(-5));

		// only message 2 carries an attachment
		h.AttachmentRepository.SeedChannelAttachment(channelId: 100, messageId: 2,
			new AttachmentMetadata(555, "http://localhost/api/chat/v1/attachments/555/pic.png", "pic.png", 1024, "image/png"));

		var result = await handler.HandleAsync(new GetChannelMessagesQuery(ChannelId: 100, BeforeTime: anchor, Limit: 50));

		Assert.True(result.Succeeded);
		var withAttachment = Assert.Single(result.Value, m => m.Id == "2");
		var attachment = Assert.Single(withAttachment.Attachments);
		Assert.Equal("555", attachment.Id);
		Assert.Equal("pic.png", attachment.Filename);

		// the other message hydrates to an empty list, not null
		var withoutAttachment = Assert.Single(result.Value, m => m.Id == "1");
		Assert.Empty(withoutAttachment.Attachments);
	}

	[Fact]
	public async Task HydratesReactions_PerMessage_WithMeReactedForCaller()
	{
		var (h, handler) = BuildHandler(userId: 42);
		h.GuildClient.Result = new ChannelMembership(IsMember: true, GuildId: 5, Permissions: ReadMessagesPermission);

		var anchor = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
		SeedMessage(h.Repository, id: 1, channelId: 100, createdAt: anchor.AddMinutes(-10));
		SeedMessage(h.Repository, id: 2, channelId: 100, createdAt: anchor.AddMinutes(-5));

		// message 1: the caller reacted; message 2: someone else reacted, caller didn't
		h.ReactionRepository.Seed(channelId: 100, messageId: 1, emoji: "👍", userId: 42);
		h.ReactionRepository.Seed(channelId: 100, messageId: 2, emoji: "🔥", userId: 7);

		var result = await handler.HandleAsync(new GetChannelMessagesQuery(ChannelId: 100, BeforeTime: anchor, Limit: 50));

		Assert.True(result.Succeeded);

		var withMyReaction = Assert.Single(result.Value, m => m.Id == "1");
		var reaction1 = Assert.Single(withMyReaction.Reactions);
		Assert.Equal("👍", reaction1.Emoji);
		Assert.Equal(1, reaction1.Count);
		Assert.True(reaction1.MeReacted);

		var withOthersReaction = Assert.Single(result.Value, m => m.Id == "2");
		var reaction2 = Assert.Single(withOthersReaction.Reactions);
		Assert.Equal("🔥", reaction2.Emoji);
		Assert.Equal(1, reaction2.Count);
		Assert.False(reaction2.MeReacted);
	}

	[Fact]
	public async Task AddThenRemoveReaction_ReturnsNoReactions_NoGhostZeroCountEntry()
	{
		var (h, handler) = BuildHandler(userId: 42);
		h.GuildClient.Result = new ChannelMembership(IsMember: true, GuildId: 5, Permissions: ReadMessagesPermission);

		var anchor = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
		SeedMessage(h.Repository, id: 1, channelId: 100, createdAt: anchor.AddMinutes(-10));

		await h.ReactionRepository.AddAsync(channelId: 100, messageId: 1, userId: 42, emoji: "👍", now: anchor, ct: default);
		await h.ReactionRepository.RemoveAsync(channelId: 100, messageId: 1, userId: 42, emoji: "👍", ct: default);

		var result = await handler.HandleAsync(new GetChannelMessagesQuery(ChannelId: 100, BeforeTime: anchor, Limit: 50));

		Assert.True(result.Succeeded);
		var message = Assert.Single(result.Value, m => m.Id == "1");
		Assert.Empty(message.Reactions);
	}

	[Fact]
	public async Task Administrator_BypassesReadPermissionCheck()
	{
		var (h, handler) = BuildHandler();
		h.GuildClient.Result = new ChannelMembership(IsMember: true, GuildId: 5, Permissions: AdministratorPermission);
		h.Clock.Set(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero));

		var result = await handler.HandleAsync(new GetChannelMessagesQuery(ChannelId: 100, BeforeTime: null, Limit: 50));

		Assert.True(result.Succeeded);
	}
}
