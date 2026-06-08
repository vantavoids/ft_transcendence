using Chat.Application.Abstractions.Messaging;
using Chat.Application.Features.Messages.Common;
using Chat.Application.Features.Messages.EditMessage;
using Chat.Domain.Messages;
using Chat.Domain.Results;
using Chat.UnitTests.Fakes;
using Xunit;

namespace Chat.UnitTests.Application;

public sealed class EditMessageHandlerTests
{
	private sealed record Harness(
		FakeCurrentUser CurrentUser,
		FakeMessageRepository Repository,
		FakeClock Clock,
		FakeChannelBroadcaster Broadcaster);

	private static (Harness Harness, ICommandHandler<EditMessageCommand, Result<EditMessageResponse>> Handler)
		BuildHandler(long userId = 42)
	{
		var currentUser = new FakeCurrentUser { UserId = userId };
		var repository = new FakeMessageRepository();
		var clock = new FakeClock();
		var broadcaster = new FakeChannelBroadcaster();

		var handler = HandlerFactory.CreateCommand<EditMessageCommand, Result<EditMessageResponse>>(
			currentUser, repository, clock, broadcaster);

		return (new Harness(currentUser, repository, clock, broadcaster), handler);
	}

	private static Message SeedMessage(FakeMessageRepository repo, long id = 1, long channelId = 100, long authorId = 42) =>
		SeedMessage(repo, id, channelId, authorId, isDeleted: false);

	private static Message SeedMessage(FakeMessageRepository repo, long id, long channelId, long authorId, bool isDeleted)
	{
		var message = Message.Reconstitute(
			id: id,
			channelId: channelId,
			authorId: authorId,
			content: "original content",
			replyToId: null,
			editedAt: null,
			isDeleted: isDeleted,
			createdAt: DateTimeOffset.UtcNow);
		repo.Seed(message);
		return message;
	}

	[Fact]
	public async Task MessageNotFound_ReturnsNotFound_NoSideEffects()
	{
		var (h, handler) = BuildHandler();

		var result = await handler.HandleAsync(new EditMessageCommand(MessageId: 999, Content: "new content"));

		Assert.True(result.IsFailure);
		Assert.Equal(MessageFailures.NotFound, result.Error);
		Assert.Empty(h.Repository.Updated);
		Assert.Empty(h.Broadcaster.EditedBroadcasts);
	}

	[Fact]
	public async Task DeletedMessage_ReturnsNotFound_NoSideEffects()
	{
		var (h, handler) = BuildHandler(userId: 42);
		SeedMessage(h.Repository, id: 1, channelId: 100, authorId: 42, isDeleted: true);

		var result = await handler.HandleAsync(new EditMessageCommand(MessageId: 1, Content: "new content"));

		Assert.True(result.IsFailure);
		Assert.Equal(MessageFailures.NotFound, result.Error);
		Assert.Empty(h.Repository.Updated);
	}

	[Fact]
	public async Task NotAuthor_ReturnsNotAuthor_NoSideEffects()
	{
		var (h, handler) = BuildHandler(userId: 42);
		SeedMessage(h.Repository, id: 1, channelId: 100, authorId: 99);

		var result = await handler.HandleAsync(new EditMessageCommand(MessageId: 1, Content: "new content"));

		Assert.True(result.IsFailure);
		Assert.Equal(MessageFailures.NotAuthor, result.Error);
		Assert.Empty(h.Repository.Updated);
		Assert.Empty(h.Broadcaster.EditedBroadcasts);
	}

	[Fact]
	public async Task EmptyContent_ReturnsContentRequired_NoSideEffects()
	{
		var (h, handler) = BuildHandler(userId: 42);
		SeedMessage(h.Repository);

		var result = await handler.HandleAsync(new EditMessageCommand(MessageId: 1, Content: "   "));

		Assert.True(result.IsFailure);
		Assert.Equal(MessageFailures.ContentRequired, result.Error);
		Assert.Empty(h.Repository.Updated);
		Assert.Empty(h.Broadcaster.EditedBroadcasts);
	}

	[Fact]
	public async Task ContentTooLong_ReturnsContentTooLong_NoSideEffects()
	{
		var (h, handler) = BuildHandler(userId: 42);
		SeedMessage(h.Repository);

		var result = await handler.HandleAsync(new EditMessageCommand(MessageId: 1, Content: new string('x', 4001)));

		Assert.True(result.IsFailure);
		Assert.Equal(MessageFailures.ContentTooLong, result.Error);
		Assert.Empty(h.Repository.Updated);
	}

	[Fact]
	public async Task HappyPath_UpdatesRepo_Broadcasts_ReturnsResponse()
	{
		var (h, handler) = BuildHandler(userId: 42);
		SeedMessage(h.Repository, id: 1, channelId: 100, authorId: 42);

		var result = await handler.HandleAsync(new EditMessageCommand(MessageId: 1, Content: "updated content"));

		Assert.True(result.Succeeded);
		var response = result.Value;
		Assert.Equal("1", response.Id);
		Assert.Equal("updated content", response.Content);
		Assert.True(response.EditedAt > DateTimeOffset.MinValue);

		var updated = Assert.Single(h.Repository.Updated);
		Assert.Equal(1L, updated.Id);
		Assert.Equal("updated content", updated.Content);

		var (broadcastChannel, evt) = Assert.Single(h.Broadcaster.EditedBroadcasts);
		Assert.Equal(100L, broadcastChannel);
		Assert.Equal("1", evt.Id);
		Assert.Equal("100", evt.ChannelId);
		Assert.Equal("updated content", evt.Content);
	}
}
