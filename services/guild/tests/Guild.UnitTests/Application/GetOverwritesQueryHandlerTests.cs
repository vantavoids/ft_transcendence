using Guild.Application.Features.Channels.Permissions.GetOverwrites;
using Guild.Domain.Guild;
using Guild.Domain.Results;
using Guild.UnitTests.Fakes;
using Xunit;
using GuildEntity = Guild.Domain.Guild.Guild;

namespace Guild.UnitTests.Application;

public sealed class GetOverwritesQueryHandlerTests
{
	private static readonly DateTimeOffset Now =
		new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

	[Fact]
	public async Task UnknownChannel_ReturnsChannelNotFound()
	{
		var (handler, _, _, _) = MakeHandler(currentUser: 1);

		var result = await handler.HandleAsync(new GetOverwritesQuery(999));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.ChannelNotFound", result.Error.Code);
	}

	[Fact]
	public async Task NonMember_ReturnsNotAMember()
	{
		// contract: "Caller must be a member of the guild that owns the channel"
		// non-member must not be able to read overwrites at all
		var (handler, _, channels, _) = MakeHandler(currentUser: 9999);
		channels.Seed(Channel.Create(5, 100, null, "g", null, ChannelType.Text, 0, Now).Value);

		var result = await handler.HandleAsync(new GetOverwritesQuery(5));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.NotAMember", result.Error.Code);
	}

	[Fact]
	public async Task MemberWithoutManageChannels_ReturnsMissingPermission()
	{
		// contract: "and have MANAGE_CHANNELS"
		var (handler, guilds, channels, _) = MakeHandler(currentUser: 2);
		channels.Seed(Channel.Create(5, 100, null, "g", null, ChannelType.Text, 0, Now).Value);
		DomainSeed.AddMember(guilds.Store[100], userId: 2, joinedAt: Now);

		var result = await handler.HandleAsync(new GetOverwritesQuery(5));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.MissingPermission", result.Error.Code);
	}

	[Fact]
	public async Task Owner_ReturnsEmptyListWhenNoOverwrites()
	{
		// owner short-circuits the permission check; empty list is a valid response
		var (handler, _, channels, _) = MakeHandler(currentUser: 1);
		channels.Seed(Channel.Create(5, 100, null, "g", null, ChannelType.Text, 0, Now).Value);

		var result = await handler.HandleAsync(new GetOverwritesQuery(5));

		Assert.True(result.Succeeded);
		Assert.Empty(result.Value.Items);
	}

	[Fact]
	public async Task Owner_ReturnsAllOverwritesForChannel()
	{
		var (handler, guilds, channels, overwrites) = MakeHandler(currentUser: 1);
		channels.Seed(Channel.Create(5, 100, null, "g", null, ChannelType.Text, 0, Now).Value);
		var everyoneId = guilds.Store[100].Roles.First(r => r.IsDefault).Id;

		overwrites.Seed(ChannelPermissionOverwrite.Create(
			id: 1, guildId: 100, channelId: 5, targetType: OverwriteTargetType.Role,
			targetId: everyoneId, allow: 2L, deny: 0L, now: Now).Value);
		overwrites.Seed(ChannelPermissionOverwrite.Create(
			id: 2, guildId: 100, channelId: 5, targetType: OverwriteTargetType.Member,
			targetId: 1, allow: 0L, deny: 4L, now: Now).Value);

		var result = await handler.HandleAsync(new GetOverwritesQuery(5));

		Assert.True(result.Succeeded);
		Assert.Equal(2, result.Value.Items.Count);
	}

	[Fact]
	public async Task Response_RoleTargetType_SerialisesAsRole()
	{
		// contract: target_type is "role" or "user" (not "member")
		var (handler, guilds, channels, overwrites) = MakeHandler(currentUser: 1);
		channels.Seed(Channel.Create(5, 100, null, "g", null, ChannelType.Text, 0, Now).Value);
		var everyoneId = guilds.Store[100].Roles.First(r => r.IsDefault).Id;
		overwrites.Seed(ChannelPermissionOverwrite.Create(
			id: 1, guildId: 100, channelId: 5, targetType: OverwriteTargetType.Role,
			targetId: everyoneId, allow: 2L, deny: 0L, now: Now).Value);

		var result = await handler.HandleAsync(new GetOverwritesQuery(5));

		Assert.True(result.Succeeded);
		Assert.Equal("role", result.Value.Items.Single().TargetType);
	}

	[Fact]
	public async Task Response_MemberTargetType_SerialisesAsUser()
	{
		// contract: domain "Member" enum value must serialise as wire "user"
		var (handler, _, channels, overwrites) = MakeHandler(currentUser: 1);
		channels.Seed(Channel.Create(5, 100, null, "g", null, ChannelType.Text, 0, Now).Value);
		overwrites.Seed(ChannelPermissionOverwrite.Create(
			id: 1, guildId: 100, channelId: 5, targetType: OverwriteTargetType.Member,
			targetId: 1, allow: 0L, deny: 4L, now: Now).Value);

		var result = await handler.HandleAsync(new GetOverwritesQuery(5));

		Assert.True(result.Succeeded);
		Assert.Equal("user", result.Value.Items.Single().TargetType);
	}

	[Fact]
	public async Task Response_TargetIdEmittedAsString()
	{
		// snowflake IDs must appear as quoted strings on the wire
		var (handler, guilds, channels, overwrites) = MakeHandler(currentUser: 1);
		channels.Seed(Channel.Create(5, 100, null, "g", null, ChannelType.Text, 0, Now).Value);
		var everyoneId = guilds.Store[100].Roles.First(r => r.IsDefault).Id;
		overwrites.Seed(ChannelPermissionOverwrite.Create(
			id: 1, guildId: 100, channelId: 5, targetType: OverwriteTargetType.Role,
			targetId: everyoneId, allow: 2L, deny: 0L, now: Now).Value);

		var result = await handler.HandleAsync(new GetOverwritesQuery(5));

		Assert.True(result.Succeeded);
		Assert.Equal(everyoneId.ToString(), result.Value.Items.Single().TargetId);
	}

	[Fact]
	public async Task Response_PreservesAllowAndDenyBitmasks()
	{
		var (handler, guilds, channels, overwrites) = MakeHandler(currentUser: 1);
		channels.Seed(Channel.Create(5, 100, null, "g", null, ChannelType.Text, 0, Now).Value);
		var everyoneId = guilds.Store[100].Roles.First(r => r.IsDefault).Id;
		overwrites.Seed(ChannelPermissionOverwrite.Create(
			id: 1, guildId: 100, channelId: 5, targetType: OverwriteTargetType.Role,
			targetId: everyoneId, allow: 7L, deny: 8L, now: Now).Value);

		var result = await handler.HandleAsync(new GetOverwritesQuery(5));

		Assert.True(result.Succeeded);
		var item = result.Value.Items.Single();
		Assert.Equal(7L, item.Allow);
		Assert.Equal(8L, item.Deny);
	}

	[Fact]
	public async Task MemberWithManageChannels_CanList()
	{
		// non-owner with MANAGE_CHANNELS should also be able to list
		var (handler, guilds, channels, overwrites) = MakeHandler(currentUser: 2);
		channels.Seed(Channel.Create(5, 100, null, "g", null, ChannelType.Text, 0, Now).Value);

		var guild = guilds.Store[100];
		var manageChannelsRole = DomainSeed.AddCustomRole(
			guild, roleId: 200, name: "ChannelMod",
			permissions: (long)Permission.ManageChannels,
			position: 2, now: Now);
		DomainSeed.AddMember(guild, userId: 2, joinedAt: Now);
		DomainSeed.AssignRole(guild, userId: 2, roleId: manageChannelsRole.Id, now: Now);

		var everyoneId = guild.Roles.First(r => r.IsDefault).Id;
		overwrites.Seed(ChannelPermissionOverwrite.Create(
			id: 1, guildId: 100, channelId: 5, targetType: OverwriteTargetType.Role,
			targetId: everyoneId, allow: 2L, deny: 0L, now: Now).Value);

		var result = await handler.HandleAsync(new GetOverwritesQuery(5));

		Assert.True(result.Succeeded);
		Assert.Single(result.Value.Items);
	}

	private static (
		Guild.Application.Abstractions.Messaging.IQueryHandler<GetOverwritesQuery, Result<OverwritesResponse>> Handler,
		FakeGuildRepository Guilds,
		FakeChannelRepository Channels,
		FakeChannelPermissionOverwriteRepository Overwrites)
		MakeHandler(long currentUser)
	{
		var guilds = new FakeGuildRepository();
		var channels = new FakeChannelRepository();
		var overwrites = new FakeChannelPermissionOverwriteRepository();
		var guild = GuildEntity.Create(
			id: 100, name: "Test", description: null, iconUrl: null, bannerUrl: null,
			ownerId: 1, everyoneRoleId: 101, adminRoleId: 102, now: Now).Value;
		guilds.AddAsync(guild).GetAwaiter().GetResult();

		var handler = HandlerFactory.CreateQuery<GetOverwritesQuery, Result<OverwritesResponse>>(
			guilds, channels, overwrites, new FakeCurrentUser { Id = currentUser });
		return (handler, guilds, channels, overwrites);
	}
}
