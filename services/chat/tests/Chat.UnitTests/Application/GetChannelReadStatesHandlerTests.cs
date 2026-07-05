using Chat.Application.Abstractions;
using Chat.Application.Abstractions.Messaging;
using Chat.Application.Features.Channels.Common;
using Chat.Application.Features.Channels.GetReadStates;
using Chat.Domain.ReadStates;
using Chat.Domain.Results;
using Chat.UnitTests.Fakes;
using Xunit;

namespace Chat.UnitTests.Application;

public sealed class GetChannelReadStatesHandlerTests
{
	private sealed record Harness(FakeCurrentUser CurrentUser, FakeGuildClient GuildClient, FakeReadStateRepository ReadStates);

	private static (Harness Harness, IQueryHandler<GetChannelReadStatesQuery, Result<IReadOnlyList<ChannelReadStateResponse>>> Handler)
		BuildHandler(long userId = 42)
	{
		var currentUser = new FakeCurrentUser { UserId = userId };
		var guildClient = new FakeGuildClient();
		var readStates = new FakeReadStateRepository();

		var handler = HandlerFactory.CreateQuery<GetChannelReadStatesQuery, Result<IReadOnlyList<ChannelReadStateResponse>>>(
			currentUser, guildClient, readStates);

		return (new Harness(currentUser, guildClient, readStates), handler);
	}

	[Fact]
	public async Task NoVisibleChannels_ReturnsEmptyList()
	{
		var (_, handler) = BuildHandler();

		var result = await handler.HandleAsync(new GetChannelReadStatesQuery());

		Assert.True(result.Succeeded);
		Assert.Empty(result.Value);
	}

	[Fact]
	public async Task ChannelWithNoReadStateRow_ReturnsNullCursor_AndTotalCount()
	{
		var (h, handler) = BuildHandler(userId: 42);
		h.GuildClient.VisibleChannelIds = [100];
		h.ReadStates.ChannelMessageCountsAfter[100] = 7;

		var result = await handler.HandleAsync(new GetChannelReadStatesQuery());

		Assert.True(result.Succeeded);
		var entry = Assert.Single(result.Value);
		Assert.Equal("100", entry.ChannelId);
		Assert.Null(entry.LastReadMessageId);
		Assert.Equal(7, entry.UnreadCount);
	}

	[Fact]
	public async Task ChannelWithReadStateRow_ReturnsCursor_AndCountSinceCursor()
	{
		var (h, handler) = BuildHandler(userId: 42);
		h.GuildClient.VisibleChannelIds = [100];
		h.ReadStates.SeedChannelReadState(42, new ReadState(
			ContainerId: 100, IsDm: false, LastReadMessageId: 5,
			LastReadAt: new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero)));
		h.ReadStates.ChannelMessageCountsAfter[100] = 2;

		var result = await handler.HandleAsync(new GetChannelReadStatesQuery());

		Assert.True(result.Succeeded);
		var entry = Assert.Single(result.Value);
		Assert.Equal("100", entry.ChannelId);
		Assert.Equal("5", entry.LastReadMessageId);
		Assert.Equal(2, entry.UnreadCount);
	}

	[Fact]
	public async Task MultipleVisibleChannels_ReturnsOneEntryEach()
	{
		var (h, handler) = BuildHandler(userId: 42);
		h.GuildClient.VisibleChannelIds = [100, 200];

		var result = await handler.HandleAsync(new GetChannelReadStatesQuery());

		Assert.True(result.Succeeded);
		Assert.Equal(2, result.Value.Count);
		Assert.Contains(result.Value, r => r.ChannelId == "100");
		Assert.Contains(result.Value, r => r.ChannelId == "200");
	}
}
