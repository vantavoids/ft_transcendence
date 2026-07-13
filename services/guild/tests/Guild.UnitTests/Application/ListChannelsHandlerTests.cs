using Guild.Application.Abstractions.Messaging;
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
		var handler = MakeHandler(guildRepo, new FakeChannelRepository(), userId: 1);

		var result = await handler.HandleAsync(new ListChannelsQuery(GuildId: 999));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.GuildNotFound", result.Error.Code);
	}

	[Fact]
	public async Task NonMember_ReturnsNotAMember()
	{
		var guildRepo = new FakeGuildRepository();
		Seed(guildRepo, ownerId: 1);
		var handler = MakeHandler(guildRepo, new FakeChannelRepository(), userId: 99);

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

		var handler = MakeHandler(guildRepo, channelRepo, userId: 1);

		var result = await handler.HandleAsync(new ListChannelsQuery(GuildId: 100));

		Assert.True(result.Succeeded);
		Assert.Equal(2, result.Value.Items.Count);
		Assert.Equal("a", result.Value.Items[0].Name);
		Assert.Equal("b", result.Value.Items[1].Name);
	}

	[Fact]
	public async Task Member_DeniedReadOnChannel_HasItFilteredOut()
	{
		var guildRepo = new FakeGuildRepository();
		var guild = Seed(guildRepo, ownerId: 1);
		DomainSeed.AddMember(guild, userId: 2, joinedAt: Now);

		var channelRepo = new FakeChannelRepository();
		channelRepo.Seed(Channel.Create(1, 100, null, "general", null, ChannelType.Text, 0, Now).Value);
		channelRepo.Seed(Channel.Create(2, 100, null, "secret", null, ChannelType.Text, 1, Now).Value);

		var overwrites = new FakeChannelPermissionOverwriteRepository();
		overwrites.Seed(ChannelPermissionOverwrite.Create(
			id: 1, guildId: 100, channelId: 2, targetType: OverwriteTargetType.Member,
			targetId: 2, allow: 0L, deny: (long)Permission.ReadMessages, now: Now).Value);

		var handler = HandlerFactory.CreateQuery<ListChannelsQuery, Result<ChannelListResponse>>(
			guildRepo, channelRepo, overwrites, new FakeCurrentUser { Id = 2 });

		var result = await handler.HandleAsync(new ListChannelsQuery(GuildId: 100));

		Assert.True(result.Succeeded);
		Assert.Equal("general", Assert.Single(result.Value.Items).Name);
	}

	private static IQueryHandler<ListChannelsQuery, Result<ChannelListResponse>> MakeHandler(
		FakeGuildRepository guilds, FakeChannelRepository channels, long userId) =>
		HandlerFactory.CreateQuery<ListChannelsQuery, Result<ChannelListResponse>>(
			guilds, channels, new FakeChannelPermissionOverwriteRepository(), new FakeCurrentUser { Id = userId });

	private static GuildEntity Seed(FakeGuildRepository repo, long ownerId)
	{
		var guild = GuildEntity.Create(
			id: 100, name: "Test", description: null, iconUrl: null, bannerUrl: null,
			ownerId: ownerId, everyoneRoleId: 101, adminRoleId: 102, now: Now).Value;
		repo.Add(guild);
		return guild;
	}
}
