using Guild.Application.Features.Channels.Membership;
using Guild.Domain.Guild;
using Guild.Domain.Results;
using Guild.UnitTests.Fakes;
using Xunit;
using GuildEntity = Guild.Domain.Guild.Guild;

namespace Guild.UnitTests.Application;

public sealed class GetChannelMembershipHandlerTests
{
	private static readonly DateTimeOffset Now =
		new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

	[Fact]
	public async Task UnknownChannel_ReturnsChannelNotFound()
	{
		var (handler, _, _, _) = MakeHandler();

		var result = await handler.HandleAsync(new GetChannelMembershipQuery(999, 1));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.ChannelNotFound", result.Error.Code);
	}

	[Fact]
	public async Task NonMember_ReturnsResponseWithGuildIdAndZeroPermissions()
	{
		var (handler, _, channels, _) = MakeHandler();
		channels.Seed(Channel.Create(5, 100, null, "g", null, ChannelType.Text, 0, Now).Value);

		var result = await handler.HandleAsync(new GetChannelMembershipQuery(5, 9999));

		Assert.True(result.Succeeded);
		Assert.False(result.Value.IsMember);
		Assert.Equal("100", result.Value.GuildId);
		Assert.Equal(0L, result.Value.Permissions);
	}

	[Fact]
	public async Task Owner_ReturnsAllBits()
	{
		var (handler, _, channels, _) = MakeHandler();
		channels.Seed(Channel.Create(5, 100, null, "g", null, ChannelType.Text, 0, Now).Value);

		var result = await handler.HandleAsync(new GetChannelMembershipQuery(5, 1));

		Assert.True(result.Succeeded);
		Assert.True(result.Value.IsMember);
		Assert.Equal("100", result.Value.GuildId);
		Assert.Equal(~0L, result.Value.Permissions);
	}

	[Fact]
	public async Task Member_AppliesOverwrites()
	{
		var (handler, guilds, channels, overwrites) = MakeHandler();
		var guild = guilds.Store[100];
		DomainSeed.AddMember(guild, userId: 2, joinedAt: Now);
		channels.Seed(Channel.Create(5, 100, null, "g", null, ChannelType.Text, 0, Now).Value);

		// member overwrite denies ReadMessages bit (2)
		overwrites.Seed(ChannelPermissionOverwrite.Create(
			id: 1, guildId: 100, channelId: 5, targetType: OverwriteTargetType.Member,
			targetId: 2, allow: 0L, deny: (long)Permission.ReadMessages, now: Now).Value);

		var result = await handler.HandleAsync(new GetChannelMembershipQuery(5, 2));

		Assert.True(result.Succeeded);
		Assert.True(result.Value.IsMember);
		Assert.Equal(0L, result.Value.Permissions & (long)Permission.ReadMessages);
	}

	private static (
		Guild.Application.Abstractions.Messaging.IQueryHandler<GetChannelMembershipQuery, Result<MembershipResponse>> Handler,
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
		guilds.AddAsync(guild).GetAwaiter().GetResult();

		var handler = HandlerFactory.CreateQuery<GetChannelMembershipQuery, Result<MembershipResponse>>(
			guilds, channels, overwrites);
		return (handler, guilds, channels, overwrites);
	}
}
