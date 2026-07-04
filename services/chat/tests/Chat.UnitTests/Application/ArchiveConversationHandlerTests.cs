using Chat.Application.Abstractions.Messaging;
using Chat.Application.Features.DirectMessages.ArchiveConversation;
using Chat.Domain.Conversations;
using Chat.Domain.Results;
using Chat.UnitTests.Fakes;
using Xunit;

namespace Chat.UnitTests.Application;

public sealed class ArchiveConversationHandlerTests
{
	private sealed record Harness(FakeCurrentUser CurrentUser, FakeMessageRepository Repository);

	private static (Harness Harness, ICommandHandler<ArchiveConversationCommand, Result> Handler)
		BuildHandler(long userId = 42)
	{
		var currentUser = new FakeCurrentUser { UserId = userId };
		var repository = new FakeMessageRepository();

		var handler = HandlerFactory.CreateCommand<ArchiveConversationCommand, Result>(currentUser, repository);

		return (new Harness(currentUser, repository), handler);
	}

	[Fact]
	public async Task NoConversation_ReturnsConversationNotFound()
	{
		var (_, handler) = BuildHandler();

		var result = await handler.HandleAsync(new ArchiveConversationCommand(PartnerId: 100));

		Assert.True(result.IsFailure);
		Assert.Equal(MessageFailures.ConversationNotFound, result.Error);
	}

	[Fact]
	public async Task ExistingConversation_FlipsIsArchived_ForCallerOnly()
	{
		var (h, handler) = BuildHandler();

		h.Repository.WithConversation(42, 100, conversationId: 999);
		h.Repository.WithConversationSummary(42, new DmConversation(
			PartnerId: 100, LastMessageAt: DateTimeOffset.UtcNow, LastPreview: "hey", IsArchived: false));
		h.Repository.WithConversationSummary(100, new DmConversation(
			PartnerId: 42, LastMessageAt: DateTimeOffset.UtcNow, LastPreview: "hey", IsArchived: false));

		var result = await handler.HandleAsync(new ArchiveConversationCommand(PartnerId: 100));

		Assert.True(result.Succeeded);

		var callerSide = Assert.Single(await h.Repository.GetConversationsAsync(42, default));
		Assert.True(callerSide.IsArchived);

		var partnerSide = Assert.Single(await h.Repository.GetConversationsAsync(100, default));
		Assert.False(partnerSide.IsArchived);
	}

	[Fact]
	public async Task AlreadyArchived_IsIdempotent()
	{
		var (h, handler) = BuildHandler();

		h.Repository.WithConversation(42, 100, conversationId: 999);
		h.Repository.WithConversationSummary(42, new DmConversation(
			PartnerId: 100, LastMessageAt: DateTimeOffset.UtcNow, LastPreview: "hey", IsArchived: true));

		var result = await handler.HandleAsync(new ArchiveConversationCommand(PartnerId: 100));

		Assert.True(result.Succeeded);
		Assert.True(Assert.Single(await h.Repository.GetConversationsAsync(42, default)).IsArchived);
	}
}
