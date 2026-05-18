using Chat.Application.Abstractions;
using Chat.Application.Contracts;
using Chat.Application.Features.Messages.Common;
using Chat.Application.Features.Messages.SendMessage;
using Chat.Domain.Results;
using Chat.UnitTests.Fakes;
using Xunit;

namespace Chat.UnitTests.Application;

public sealed class SendMessageHandlerTests
{
	// matches the constants in SendMessageHandler
	private const long SendMessagesPermission = 1L << 0;
	private const long AdministratorPermission = 1L << 8;

	private sealed record Harness(
		FakeCurrentUser CurrentUser,
		FakeGuildClient GuildClient,
		FakeMessageRepository Repository,
		FakeIdGenerator IdGenerator,
		FakeClock Clock,
		FakeEventBus EventBus,
		FakeChannelBroadcaster Broadcaster);

	private static (Harness Harness,
		Chat.Application.Abstractions.Messaging.ICommandHandler<SendMessageCommand, Result<MessageResponse>> Handler)
		BuildHandler(long userId = 42)
	{
		var currentUser = new FakeCurrentUser { UserId = userId };
		var guildClient = new FakeGuildClient();
		var repository = new FakeMessageRepository();
		var ids = new FakeIdGenerator();
		var clock = new FakeClock();
		var eventBus = new FakeEventBus();
		var broadcaster = new FakeChannelBroadcaster();

		var handler = HandlerFactory.CreateCommand<SendMessageCommand, Result<MessageResponse>>(
			currentUser, guildClient, repository, ids, clock, eventBus, broadcaster);

		return (new Harness(currentUser, guildClient, repository, ids, clock, eventBus, broadcaster), handler);
	}

	[Fact]
	public async Task ChannelNotFound_ReturnsChannelNotFound_NoSideEffects()
	{
		var (h, handler) = BuildHandler();
		h.GuildClient.Result = null;

		var result = await handler.HandleAsync(new SendMessageCommand(ChannelId: 100, Content: "hi", ReplyToId: null));

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

		var result = await handler.HandleAsync(new SendMessageCommand(ChannelId: 100, Content: "hi", ReplyToId: null));

		Assert.True(result.IsFailure);
		Assert.Equal(MessageFailures.NotAMember, result.Error);
		Assert.Empty(h.Repository.Saved);
	}

	[Fact]
	public async Task MissingSendPermission_ReturnsMissingSendPermission_NoSideEffects()
	{
		var (h, handler) = BuildHandler();
		h.GuildClient.Result = new ChannelMembership(IsMember: true, GuildId: 5, Permissions: 0);

		var result = await handler.HandleAsync(new SendMessageCommand(ChannelId: 100, Content: "hi", ReplyToId: null));

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

		var result = await handler.HandleAsync(new SendMessageCommand(ChannelId: 100, Content: "hi", ReplyToId: null));

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

		var result = await handler.HandleAsync(new SendMessageCommand(ChannelId: 100, Content: "hello", ReplyToId: 7));

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

		// repo persisted the new message
		var saved = Assert.Single(h.Repository.Saved);
		Assert.Equal(responseId, saved.Id);
		Assert.Equal(100L, saved.ChannelId);
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
	public async Task ContentRequired_DomainFailureBubblesUp_NoSideEffects()
	{
		var (h, handler) = BuildHandler();
		h.GuildClient.Result = new ChannelMembership(IsMember: true, GuildId: 5, Permissions: SendMessagesPermission);

		var result = await handler.HandleAsync(new SendMessageCommand(ChannelId: 100, Content: "   ", ReplyToId: null));

		Assert.True(result.IsFailure);
		Assert.Equal(MessageFailures.ContentRequired, result.Error);
		Assert.Empty(h.Repository.Saved);
		Assert.Empty(h.EventBus.Published);
		Assert.Empty(h.Broadcaster.Broadcasts);
	}
}
