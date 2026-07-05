using Guild.Application.Features.Channels.Common;
using Guild.Application.Features.Channels.ListChannels;
using Guild.Application.Features.Channels.VisibleChannels;
using Guild.Domain.Guild;
using Guild.Domain.Results;
using Guild.UnitTests.Fakes;
using Xunit;
using GuildEntity = Guild.Domain.Guild.Guild;

namespace Guild.UnitTests.Application;

public sealed class GetVisibleChannelsHandlerTests
{
	private static readonly DateTimeOffset Now =
		new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

	[Fact]
	public async Task GuildLessUser_ReturnsEmptyList()
	{
		var (handler, _, _, _) = MakeHandler();

		var result = await handler.HandleAsync(new GetVisibleChannelsQuery(9999));

		Assert.True(result.Succeeded);
		Assert.Empty(result.Value.Items);
	}

	[Fact]
	public async Task Owner_SeesAllGuildChannels()
	{
		var (handler, guilds, channels, _) = MakeHandler();
		var guild = guilds.Store[100];
		channels.Seed(Channel.Create(5, 100, null, "general", null, ChannelType.Text, 0, Now).Value);
		channels.Seed(Channel.Create(6, 100, null, "announce", null, ChannelType.Announcement, 1, Now).Value);

		var result = await handler.HandleAsync(new GetVisibleChannelsQuery(guild.OwnerId));

		Assert.True(result.Succeeded);
		Assert.Equal(2, result.Value.Items.Count);
	}

	[Fact]
	public async Task NonMember_SeesNoChannels()
	{
		var (handler, guilds, channels, _) = MakeHandler();
		channels.Seed(Channel.Create(5, 100, null, "general", null, ChannelType.Text, 0, Now).Value);

		var result = await handler.HandleAsync(new GetVisibleChannelsQuery(424242));

		Assert.True(result.Succeeded);
		Assert.Empty(result.Value.Items);
	}

	[Fact]
	public async Task MemberDeniedReadOnOneChannel_IsFilteredOut()
	{
		var (handler, guilds, channels, overwrites) = MakeHandler();
		var guild = guilds.Store[100];
		DomainSeed.AddMember(guild, userId: 2, joinedAt: Now);
		channels.Seed(Channel.Create(5, 100, null, "general", null, ChannelType.Text, 0, Now).Value);
		channels.Seed(Channel.Create(6, 100, null, "secret", null, ChannelType.Text, 1, Now).Value);

		overwrites.Seed(ChannelPermissionOverwrite.Create(
			id: 1, guildId: 100, channelId: 6, targetType: OverwriteTargetType.Member,
			targetId: 2, allow: 0L, deny: (long)Permission.ReadMessages, now: Now).Value);

		var result = await handler.HandleAsync(new GetVisibleChannelsQuery(2));

		Assert.True(result.Succeeded);
		var single = Assert.Single(result.Value.Items);
		Assert.Equal("5", single.Id);
	}

	private static (
		Guild.Application.Abstractions.Messaging.IQueryHandler<GetVisibleChannelsQuery, Result<ChannelListResponse>> Handler,
		FakeGuildRepository Guilds,
		FakeChannelRepository Channels,
		FakeChannelPermissionOverwriteRepository Overwrites)
		MakeHandler()
	{
		var guilds = new FakeGuildRepository();
		var channels = new FakeChannelRepository();
		var overwrites = new FakeChannelPermissionOverwriteRepository();
		var guild = GuildEntity.Create(
			id: 100, name: "Test", description: null, iconUrl: null, bannerUrl: null,
			ownerId: 1, everyoneRoleId: 101, adminRoleId: 102, now: Now).Value;
		guilds.Add(guild);

		var handler = HandlerFactory.CreateQuery<GetVisibleChannelsQuery, Result<ChannelListResponse>>(
			guilds, channels, overwrites);
		return (handler, guilds, channels, overwrites);
	}
}
