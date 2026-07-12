using Guild.Application.Contracts;
using Guild.Application.Features.Channels.DeleteChannel;
using Guild.Domain.Guild;
using Guild.Domain.Results;
using Guild.UnitTests.Fakes;
using Xunit;
using GuildEntity = Guild.Domain.Guild.Guild;

namespace Guild.UnitTests.Application;

public sealed class DeleteChannelHandlerTests
{
	private static readonly DateTimeOffset Now =
		new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

	[Fact]
	public async Task UnknownChannel_ReturnsChannelNotFound()
	{
		var (handler, _, _) = MakeHandler(currentUser: 1);

		var result = await handler.HandleAsync(new DeleteChannelCommand(100, 999));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.ChannelNotFound", result.Error.Code);
	}

	[Fact]
	public async Task HappyPath_RemovesChannel()
	{
		var (handler, _, channels) = MakeHandler(currentUser: 1);
		channels.Seed(Channel.Create(5, 100, null, "g", null, ChannelType.Text, 0, Now).Value);

		var result = await handler.HandleAsync(new DeleteChannelCommand(100, 5));

		Assert.True(result.Succeeded);
		Assert.Empty(channels.Store);
	}

	[Fact]
	public async Task HappyPath_PublishesChannelDeleted()
	{
		var guilds = new FakeGuildRepository();
		var channels = new FakeChannelRepository();
		var bus = new FakeEventBus();
		var guild = GuildEntity.Create(
			id: 100, name: "Test", description: null, iconUrl: null, bannerUrl: null,
			ownerId: 1, everyoneRoleId: 101, adminRoleId: 102, now: Now).Value;
		guilds.Add(guild);
		channels.Seed(Channel.Create(5, 100, null, "g", null, ChannelType.Text, 0, Now).Value);

		var handler = HandlerFactory.CreateCommand<DeleteChannelCommand, Result>(
			guilds, channels, new FakeChannelPermissionOverwriteRepository(), bus,
			new FakeCurrentUser { Id = 1 });

		var result = await handler.HandleAsync(new DeleteChannelCommand(100, 5));

		Assert.True(result.Succeeded);
		var evt = bus.Single<GuildChannelDeleted>();
		Assert.Equal(5, evt.ChannelId);
		Assert.Contains(1L, evt.EligibleUserIds);
	}

	[Fact]
	public async Task NonMember_ReturnsNotAMember()
	{
		var (handler, _, channels) = MakeHandler(currentUser: 99);
		channels.Seed(Channel.Create(5, 100, null, "g", null, ChannelType.Text, 0, Now).Value);

		var result = await handler.HandleAsync(new DeleteChannelCommand(100, 5));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.NotAMember", result.Error.Code);
	}

	[Fact]
	public async Task MemberWithoutManageChannels_ReturnsMissingPermission()
	{
		var (handler, guilds, channels) = MakeHandler(currentUser: 2);
		DomainSeed.AddMember(guilds.Store[100], userId: 2, joinedAt: Now);
		channels.Seed(Channel.Create(5, 100, null, "g", null, ChannelType.Text, 0, Now).Value);

		var result = await handler.HandleAsync(new DeleteChannelCommand(100, 5));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.MissingPermission", result.Error.Code);
	}

	private static (
		Guild.Application.Abstractions.Messaging.ICommandHandler<DeleteChannelCommand, Result> Handler,
		FakeGuildRepository Guilds,
		FakeChannelRepository Channels)
		MakeHandler(long currentUser)
	{
		var guilds = new FakeGuildRepository();
		var channels = new FakeChannelRepository();
		var guild = GuildEntity.Create(
			id: 100, name: "Test", description: null, iconUrl: null, bannerUrl: null,
			ownerId: 1, everyoneRoleId: 101, adminRoleId: 102, now: Now).Value;
		guilds.Add(guild);

		var handler = HandlerFactory.CreateCommand<DeleteChannelCommand, Result>(
			guilds, channels, new FakeChannelPermissionOverwriteRepository(), new FakeEventBus(),
			new FakeCurrentUser { Id = currentUser });
		return (handler, guilds, channels);
	}
}
