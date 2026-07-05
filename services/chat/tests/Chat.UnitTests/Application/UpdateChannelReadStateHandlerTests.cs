using Chat.Application.Abstractions;
using Chat.Application.Abstractions.Messaging;
using Chat.Application.Features.Channels.Common;
using Chat.Application.Features.Channels.UpdateReadState;
using Chat.Domain.Messages;
using Chat.Domain.Results;
using Chat.UnitTests.Fakes;
using Xunit;

namespace Chat.UnitTests.Application;

public sealed class UpdateChannelReadStateHandlerTests
{
	private const long ReadMessagesPermission = 1L << 1;

	private sealed record Harness(
		FakeCurrentUser CurrentUser,
		FakeGuildClient GuildClient,
		FakeMessageRepository Repository,
		FakeReadStateRepository ReadStates,
		FakeUserBroadcaster Broadcaster);

	private static (Harness Harness, ICommandHandler<UpdateChannelReadStateCommand, Result<ChannelReadStateResponse>> Handler)
		BuildHandler(long userId = 42)
	{
		var currentUser = new FakeCurrentUser { UserId = userId };
		var guildClient = new FakeGuildClient();
		var repository = new FakeMessageRepository();
		var readStates = new FakeReadStateRepository();
		var broadcaster = new FakeUserBroadcaster();

		var handler = HandlerFactory.CreateCommand<UpdateChannelReadStateCommand, Result<ChannelReadStateResponse>>(
			currentUser, guildClient, repository, readStates, broadcaster);

		return (new Harness(currentUser, guildClient, repository, readStates, broadcaster), handler);
	}

	private static Message SeedMessage(FakeMessageRepository repo, long id, long channelId, DateTimeOffset createdAt)
	{
		var message = Message.Reconstitute(
			id: id, containerId: channelId, authorId: 99, recipientId: null,
			content: "hi", replyToId: null, editedAt: null, isDeleted: false, createdAt: createdAt);
		repo.Seed(message);
		return message;
	}

	[Fact]
	public async Task ChannelNotFound_ReturnsChannelNotFound()
	{
		var (h, handler) = BuildHandler();
		h.GuildClient.Result = null;

		var result = await handler.HandleAsync(new UpdateChannelReadStateCommand(ChannelId: 100, MessageId: 1));

		Assert.True(result.IsFailure);
		Assert.Equal(MessageFailures.ChannelNotFound, result.Error);
	}

	[Fact]
	public async Task NotAMember_ReturnsNotAMember()
	{
		var (h, handler) = BuildHandler();
		h.GuildClient.Result = new ChannelMembership(IsMember: false, GuildId: 5, Permissions: ReadMessagesPermission);

		var result = await handler.HandleAsync(new UpdateChannelReadStateCommand(ChannelId: 100, MessageId: 1));

		Assert.True(result.IsFailure);
		Assert.Equal(MessageFailures.NotAMember, result.Error);
	}

	[Fact]
	public async Task MissingReadPermission_ReturnsMissingReadPermission()
	{
		var (h, handler) = BuildHandler();
		h.GuildClient.Result = new ChannelMembership(IsMember: true, GuildId: 5, Permissions: 0);

		var result = await handler.HandleAsync(new UpdateChannelReadStateCommand(ChannelId: 100, MessageId: 1));

		Assert.True(result.IsFailure);
		Assert.Equal(MessageFailures.MissingReadPermission, result.Error);
	}

	[Fact]
	public async Task UnknownMessage_ReturnsNotFound()
	{
		var (h, handler) = BuildHandler();
		h.GuildClient.Result = new ChannelMembership(IsMember: true, GuildId: 5, Permissions: ReadMessagesPermission);

		var result = await handler.HandleAsync(new UpdateChannelReadStateCommand(ChannelId: 100, MessageId: 999));

		Assert.True(result.IsFailure);
		Assert.Equal(MessageFailures.NotFound, result.Error);
	}

	[Fact]
	public async Task MessageInAnotherChannel_ReturnsNotFound()
	{
		var (h, handler) = BuildHandler();
		h.GuildClient.Result = new ChannelMembership(IsMember: true, GuildId: 5, Permissions: ReadMessagesPermission);
		SeedMessage(h.Repository, id: 1, channelId: 200, createdAt: DateTimeOffset.UtcNow);

		var result = await handler.HandleAsync(new UpdateChannelReadStateCommand(ChannelId: 100, MessageId: 1));

		Assert.True(result.IsFailure);
		Assert.Equal(MessageFailures.NotFound, result.Error);
	}

	[Fact]
	public async Task HappyPath_AdvancesCursor_ReturnsZeroUnread()
	{
		var (h, handler) = BuildHandler(userId: 42);
		h.GuildClient.Result = new ChannelMembership(IsMember: true, GuildId: 5, Permissions: ReadMessagesPermission);
		var anchor = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
		SeedMessage(h.Repository, id: 1, channelId: 100, createdAt: anchor);
		h.ReadStates.ChannelMessageCountsAfter[100] = 0;

		var result = await handler.HandleAsync(new UpdateChannelReadStateCommand(ChannelId: 100, MessageId: 1));

		Assert.True(result.Succeeded);
		Assert.Equal("100", result.Value.ChannelId);
		Assert.Equal("1", result.Value.LastReadMessageId);
		Assert.Equal(0, result.Value.UnreadCount);

		var (broadcastUserId, broadcastResponse) = Assert.Single(h.Broadcaster.ReadStateCalls);
		Assert.Equal(42L, broadcastUserId);
		Assert.Same(result.Value, broadcastResponse);
	}

	[Fact]
	public async Task OlderMessageId_IsNoOp_KeepsNewerCursor()
	{
		var (h, handler) = BuildHandler(userId: 42);
		h.GuildClient.Result = new ChannelMembership(IsMember: true, GuildId: 5, Permissions: ReadMessagesPermission);
		var anchor = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
		SeedMessage(h.Repository, id: 1, channelId: 100, createdAt: anchor);
		SeedMessage(h.Repository, id: 2, channelId: 100, createdAt: anchor.AddMinutes(1));

		var first = await handler.HandleAsync(new UpdateChannelReadStateCommand(ChannelId: 100, MessageId: 2));
		Assert.True(first.Succeeded);
		Assert.Equal("2", first.Value.LastReadMessageId);

		var second = await handler.HandleAsync(new UpdateChannelReadStateCommand(ChannelId: 100, MessageId: 1));
		Assert.True(second.Succeeded);
		Assert.Equal("2", second.Value.LastReadMessageId); // unchanged, message 1 is older
	}
}
