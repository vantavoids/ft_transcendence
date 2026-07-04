using Chat.Application.Abstractions.Messaging;
using Chat.Application.Features.DirectMessages.Common;
using Chat.Application.Features.DirectMessages.ListConversations;
using Chat.Domain.Conversations;
using Chat.Domain.Results;
using Chat.UnitTests.Fakes;
using Xunit;

namespace Chat.UnitTests.Application;

public sealed class ListDmConversationsHandlerTests
{
	private sealed record Harness(FakeCurrentUser CurrentUser, FakeMessageRepository Repository);

	private static (Harness Harness, IQueryHandler<ListDmConversationsQuery, Result<IReadOnlyList<DmConversationResponse>>> Handler)
		BuildHandler(long userId = 42)
	{
		var currentUser = new FakeCurrentUser { UserId = userId };
		var repository = new FakeMessageRepository();

		var handler = HandlerFactory.CreateQuery<ListDmConversationsQuery, Result<IReadOnlyList<DmConversationResponse>>>(
			currentUser, repository);

		return (new Harness(currentUser, repository), handler);
	}

	[Fact]
	public async Task ExcludesArchived_ByDefault()
	{
		var (h, handler) = BuildHandler();

		h.Repository.WithConversationSummary(42, new DmConversation(
			PartnerId: 100, LastMessageAt: DateTimeOffset.UtcNow, LastPreview: "hey", IsArchived: false));
		h.Repository.WithConversationSummary(42, new DmConversation(
			PartnerId: 200, LastMessageAt: DateTimeOffset.UtcNow, LastPreview: "closed", IsArchived: true));

		var result = await handler.HandleAsync(new ListDmConversationsQuery(IncludeArchived: false));

		Assert.True(result.Succeeded);
		var conversation = Assert.Single(result.Value);
		Assert.Equal("100", conversation.PartnerId);
	}

	[Fact]
	public async Task IncludesArchived_WhenRequested()
	{
		var (h, handler) = BuildHandler();

		h.Repository.WithConversationSummary(42, new DmConversation(
			PartnerId: 100, LastMessageAt: DateTimeOffset.UtcNow, LastPreview: "hey", IsArchived: false));
		h.Repository.WithConversationSummary(42, new DmConversation(
			PartnerId: 200, LastMessageAt: DateTimeOffset.UtcNow, LastPreview: "closed", IsArchived: true));

		var result = await handler.HandleAsync(new ListDmConversationsQuery(IncludeArchived: true));

		Assert.True(result.Succeeded);
		Assert.Equal(2, result.Value.Count);
		Assert.Contains(result.Value, c => c.PartnerId == "200" && c.IsArchived);
	}

	[Fact]
	public async Task Sorts_ByLastMessageAt_Descending()
	{
		var (h, handler) = BuildHandler();

		var anchor = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
		h.Repository.WithConversationSummary(42, new DmConversation(
			PartnerId: 100, LastMessageAt: anchor.AddMinutes(-10), LastPreview: "older", IsArchived: false));
		h.Repository.WithConversationSummary(42, new DmConversation(
			PartnerId: 200, LastMessageAt: anchor, LastPreview: "newer", IsArchived: false));

		var result = await handler.HandleAsync(new ListDmConversationsQuery(IncludeArchived: false));

		Assert.True(result.Succeeded);
		Assert.Equal(["200", "100"], result.Value.Select(c => c.PartnerId));
	}

	[Fact]
	public async Task NoConversations_ReturnsEmptyList()
	{
		var (_, handler) = BuildHandler();

		var result = await handler.HandleAsync(new ListDmConversationsQuery(IncludeArchived: false));

		Assert.True(result.Succeeded);
		Assert.Empty(result.Value);
	}

	[Fact]
	public async Task UnreadCount_IsAlwaysZero()
	{
		var (h, handler) = BuildHandler();

		h.Repository.WithConversationSummary(42, new DmConversation(
			PartnerId: 100, LastMessageAt: DateTimeOffset.UtcNow, LastPreview: "hey", IsArchived: false));

		var result = await handler.HandleAsync(new ListDmConversationsQuery(IncludeArchived: false));

		Assert.Equal(0, Assert.Single(result.Value).UnreadCount);
	}
}
