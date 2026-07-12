using Guild.Application.Contracts;
using Guild.Application.Features.Guilds.DeleteGuild;
using Guild.Domain.Guild;
using Guild.Domain.Results;
using Guild.UnitTests.Fakes;
using Xunit;
using GuildEntity = Guild.Domain.Guild.Guild;

namespace Guild.UnitTests.Application;

public sealed class DeleteGuildHandlerTests
{
	private static readonly DateTimeOffset Now =
		new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

	[Fact]
	public async Task UnknownGuild_Returns404()
	{
		var (repo, bus) = (new FakeGuildRepository(), new FakeEventBus());
		var handler = HandlerFactory.CreateCommand<DeleteGuildCommand, Result>(
			repo, new FakeChannelRepository(), bus, new FakeCurrentUser { Id = 1 });

		var result = await handler.HandleAsync(new DeleteGuildCommand(GuildId: 999));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.GuildNotFound", result.Error.Code);
		Assert.Empty(bus.Published);
	}

	[Fact]
	public async Task NonOwner_ReturnsNotTheOwner()
	{
		var (repo, bus) = (new FakeGuildRepository(), new FakeEventBus());
		Seed(repo, ownerId: 1);
		var handler = HandlerFactory.CreateCommand<DeleteGuildCommand, Result>(
			repo, new FakeChannelRepository(), bus, new FakeCurrentUser { Id = 99 });

		var result = await handler.HandleAsync(new DeleteGuildCommand(GuildId: 100));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.NotTheOwner", result.Error.Code);
		Assert.Empty(bus.Published);
	}

	[Fact]
	public async Task Owner_Succeeds_RemovesFromStore_AndPublishesGuildDeleted()
	{
		var (repo, bus) = (new FakeGuildRepository(), new FakeEventBus());
		Seed(repo, ownerId: 1);
		var channels = new FakeChannelRepository();
		channels.Seed(Channel.Create(5, 100, null, "general", null, ChannelType.Text, 0, Now).Value);
		var handler = HandlerFactory.CreateCommand<DeleteGuildCommand, Result>(
			repo, channels, bus, new FakeCurrentUser { Id = 1 });

		var result = await handler.HandleAsync(new DeleteGuildCommand(GuildId: 100));

		Assert.True(result.Succeeded);
		Assert.Empty(repo.Store);
		var evt = bus.Single<GuildDeleted>();
		Assert.Equal(100, evt.GuildId);
		Assert.Equal(new long[] { 5 }, evt.ChannelIds);
	}

	private static void Seed(FakeGuildRepository repo, long ownerId)
	{
		var guild = GuildEntity.Create(
			id: 100,
			name: "Test",
			description: null,
			iconUrl: null,
			bannerUrl: null,
			ownerId: ownerId,
			everyoneRoleId: 101,
			adminRoleId: 102,
			now: Now).Value;
		repo.Add(guild);
	}
}
