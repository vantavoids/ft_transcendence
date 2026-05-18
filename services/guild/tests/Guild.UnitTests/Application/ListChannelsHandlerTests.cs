using Guild.Application.Features.Channels.Common;
using Guild.Application.Features.Channels.ListChannels;
using Guild.Domain.Guild;
using Guild.Domain.Results;
using Guild.UnitTests.Fakes;
using Xunit;
using GuildEntity = Guild.Domain.Guild.Guild;

namespace Guild.UnitTests.Application;

public sealed class ListChannelsHandlerTests
{
	private static readonly DateTimeOffset Now =
		new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

	[Fact]
	public async Task UnknownGuild_ReturnsGuildNotFound()
	{
		var guildRepo = new FakeGuildRepository();
		var channelRepo = new FakeChannelRepository();
		var handler = HandlerFactory.CreateQuery<ListChannelsQuery, Result<ChannelListResponse>>(
			guildRepo, channelRepo, new FakeCurrentUser { Id = 1 });

		var result = await handler.HandleAsync(new ListChannelsQuery(GuildId: 999));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.GuildNotFound", result.Error.Code);
	}

	[Fact]
	public async Task NonMember_ReturnsNotAMember()
	{
		var guildRepo = new FakeGuildRepository();
		Seed(guildRepo, ownerId: 1);
		var channelRepo = new FakeChannelRepository();
		var handler = HandlerFactory.CreateQuery<ListChannelsQuery, Result<ChannelListResponse>>(
			guildRepo, channelRepo, new FakeCurrentUser { Id = 99 });

		var result = await handler.HandleAsync(new ListChannelsQuery(GuildId: 100));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.NotAMember", result.Error.Code);
	}

	[Fact]
	public async Task Member_ReturnsChannelsOrderedByPosition()
	{
		var guildRepo = new FakeGuildRepository();
		Seed(guildRepo, ownerId: 1);
		var channelRepo = new FakeChannelRepository();
		channelRepo.Seed(Channel.Create(1, 100, null, "b", null, ChannelType.Text, 2, Now).Value);
		channelRepo.Seed(Channel.Create(2, 100, null, "a", null, ChannelType.Text, 1, Now).Value);

		var handler = HandlerFactory.CreateQuery<ListChannelsQuery, Result<ChannelListResponse>>(
			guildRepo, channelRepo, new FakeCurrentUser { Id = 1 });

		var result = await handler.HandleAsync(new ListChannelsQuery(GuildId: 100));

		Assert.True(result.Succeeded);
		Assert.Equal(2, result.Value.Items.Count);
		Assert.Equal("a", result.Value.Items[0].Name);
		Assert.Equal("b", result.Value.Items[1].Name);
	}

	private static void Seed(FakeGuildRepository repo, long ownerId)
	{
		var guild = GuildEntity.Create(
			id: 100, name: "Test", description: null, iconUrl: null, bannerUrl: null,
			ownerId: ownerId, everyoneRoleId: 101, adminRoleId: 102, now: Now).Value;
		repo.AddAsync(guild).GetAwaiter().GetResult();
	}
}
