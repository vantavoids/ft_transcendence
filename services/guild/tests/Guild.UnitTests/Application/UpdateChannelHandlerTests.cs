using Guild.Application.Features.Channels.Common;
using Guild.Application.Features.Channels.UpdateChannel;
using Guild.Domain.Guild;
using Guild.Domain.Results;
using Guild.UnitTests.Fakes;
using Xunit;
using GuildEntity = Guild.Domain.Guild.Guild;

namespace Guild.UnitTests.Application;

public sealed class UpdateChannelHandlerTests
{
	private static readonly DateTimeOffset Now =
		new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

	[Fact]
	public async Task UnknownGuild_ReturnsGuildNotFound()
	{
		var (handler, _, _) = MakeHandler(ownerSeed: null, currentUser: 1);

		var result = await handler.HandleAsync(
			new UpdateChannelCommand(999, 1, Name: null, Topic: null,
				Position: null, CategoryId: null, CategoryIdProvided: false, TopicProvided: false));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.GuildNotFound", result.Error.Code);
	}

	[Fact]
	public async Task UnknownChannel_ReturnsChannelNotFound()
	{
		var (handler, _, _) = MakeHandler(ownerSeed: 1, currentUser: 1);

		var result = await handler.HandleAsync(
			new UpdateChannelCommand(100, 999, Name: "x", Topic: null,
				Position: null, CategoryId: null, CategoryIdProvided: false, TopicProvided: false));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.ChannelNotFound", result.Error.Code);
	}

	[Fact]
	public async Task ChannelFromDifferentGuild_ReturnsChannelNotFound()
	{
		var (handler, _, channels) = MakeHandler(ownerSeed: 1, currentUser: 1);
		// channel belongs to a different guild
		channels.Seed(Channel.Create(5, 999, null, "g", null, ChannelType.Text, 0, Now).Value);

		var result = await handler.HandleAsync(
			new UpdateChannelCommand(100, 5, Name: "x", Topic: null,
				Position: null, CategoryId: null, CategoryIdProvided: false, TopicProvided: false));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.ChannelNotFound", result.Error.Code);
	}

	[Fact]
	public async Task HappyPath_RenamesAndRepositions()
	{
		var (handler, _, channels) = MakeHandler(ownerSeed: 1, currentUser: 1);
		channels.Seed(Channel.Create(5, 100, null, "old", null, ChannelType.Text, 0, Now).Value);

		var result = await handler.HandleAsync(
			new UpdateChannelCommand(100, 5, Name: "new", Topic: null,
				Position: 4, CategoryId: null, CategoryIdProvided: false, TopicProvided: false));

		Assert.True(result.Succeeded);
		Assert.Equal("new", result.Value.Name);
		Assert.Equal(4, result.Value.Position);
	}

	[Fact]
	public async Task MoveToUnknownCategory_ReturnsCategoryNotFound()
	{
		var (handler, _, channels) = MakeHandler(ownerSeed: 1, currentUser: 1);
		channels.Seed(Channel.Create(5, 100, null, "g", null, ChannelType.Text, 0, Now).Value);

		var result = await handler.HandleAsync(
			new UpdateChannelCommand(100, 5, Name: null, Topic: null,
				Position: null, CategoryId: 7777, CategoryIdProvided: true, TopicProvided: false));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.CategoryNotFound", result.Error.Code);
	}

	[Fact]
	public async Task DetachFromCategory_NullCategoryId_IsAllowed()
	{
		var (handler, _, channels) = MakeHandler(ownerSeed: 1, currentUser: 1);
		channels.Seed(Channel.Create(5, 100, 12, "g", null, ChannelType.Text, 0, Now).Value);

		var result = await handler.HandleAsync(
			new UpdateChannelCommand(100, 5, Name: null, Topic: null,
				Position: null, CategoryId: null, CategoryIdProvided: true, TopicProvided: false));

		Assert.True(result.Succeeded);
		Assert.Null(result.Value.CategoryId);
	}

	private static (
		Guild.Application.Abstractions.Messaging.ICommandHandler<UpdateChannelCommand, Result<ChannelResponse>> Handler,
		FakeGuildRepository Guilds,
		FakeChannelRepository Channels)
		MakeHandler(long? ownerSeed, long currentUser)
	{
		var guilds = new FakeGuildRepository();
		var channels = new FakeChannelRepository();
		var categories = new FakeChannelCategoryRepository();

		if (ownerSeed is long ownerId)
		{
			var guild = GuildEntity.Create(
				id: 100, name: "Test", description: null, iconUrl: null, bannerUrl: null,
				ownerId: ownerId, everyoneRoleId: 101, adminRoleId: 102, now: Now).Value;
			guilds.AddAsync(guild).GetAwaiter().GetResult();
		}

		var handler = HandlerFactory.CreateCommand<UpdateChannelCommand, Result<ChannelResponse>>(
			guilds, channels, categories,
			new FakeClock(), new FakeCurrentUser { Id = currentUser });

		return (handler, guilds, channels);
	}
}
