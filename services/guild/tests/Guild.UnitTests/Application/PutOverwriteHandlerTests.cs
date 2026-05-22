using Guild.Application.Features.Channels.Permissions.PutOverwrite;
using Guild.Domain.Guild;
using Guild.Domain.Results;
using Guild.UnitTests.Fakes;
using Xunit;
using GuildEntity = Guild.Domain.Guild.Guild;

namespace Guild.UnitTests.Application;

public sealed class PutOverwriteHandlerTests
{
	private static readonly DateTimeOffset Now =
		new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

	[Fact]
	public async Task UnknownChannel_ReturnsChannelNotFound()
	{
		var (handler, _, _, _) = MakeHandler(currentUser: 1);

		var result = await handler.HandleAsync(
			new PutOverwriteCommand(999, 50, "role", 1L, 0L));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.ChannelNotFound", result.Error.Code);
	}

	[Fact]
	public async Task InvalidTargetType_Returns400()
	{
		// uses a valid role id so the failure can only come from the type parser
		// (otherwise the test passes even if TryParseType incorrectly accepts "weird"
		// because the downstream target-existence check would reject the id anyway)
		var (handler, guilds, channels, _) = MakeHandler(currentUser: 1);
		channels.Seed(Channel.Create(5, 100, null, "g", null, ChannelType.Text, 0, Now).Value);
		var roleId = guilds.Store[100].Roles.First(r => r.IsDefault).Id;

		var result = await handler.HandleAsync(
			new PutOverwriteCommand(5, roleId, "weird", 1L, 0L));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.OverwriteInvalidTarget", result.Error.Code);
	}

	[Fact]
	public async Task NonExistentRoleTarget_ReturnsInvalidTarget()
	{
		var (handler, _, channels, _) = MakeHandler(currentUser: 1);
		channels.Seed(Channel.Create(5, 100, null, "g", null, ChannelType.Text, 0, Now).Value);

		var result = await handler.HandleAsync(
			new PutOverwriteCommand(5, 9999, "role", 1L, 0L));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.OverwriteInvalidTarget", result.Error.Code);
	}

	[Fact]
	public async Task NonExistentMemberTarget_ReturnsInvalidTarget()
	{
		var (handler, _, channels, _) = MakeHandler(currentUser: 1);
		channels.Seed(Channel.Create(5, 100, null, "g", null, ChannelType.Text, 0, Now).Value);

		var result = await handler.HandleAsync(
			new PutOverwriteCommand(5, 9999, "member", 1L, 0L));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.OverwriteInvalidTarget", result.Error.Code);
	}

	[Fact]
	public async Task HappyPath_RoleTarget_Creates()
	{
		var (handler, guilds, channels, overwrites) = MakeHandler(currentUser: 1);
		channels.Seed(Channel.Create(5, 100, null, "g", null, ChannelType.Text, 0, Now).Value);
		var roleId = guilds.Store[100].Roles.First(r => r.IsDefault).Id;

		var result = await handler.HandleAsync(
			new PutOverwriteCommand(5, roleId, "role",
				Allow: (long)Permission.SendMessages, Deny: 0L));

		Assert.True(result.Succeeded);
		Assert.Single(overwrites.Store);
		Assert.Equal(OverwriteTargetType.Role, overwrites.Store.Values.Single().TargetType);
	}

	[Fact]
	public async Task SecondPutSameTarget_UpdatesExisting()
	{
		var (handler, guilds, channels, overwrites) = MakeHandler(currentUser: 1);
		channels.Seed(Channel.Create(5, 100, null, "g", null, ChannelType.Text, 0, Now).Value);
		var roleId = guilds.Store[100].Roles.First(r => r.IsDefault).Id;

		await handler.HandleAsync(new PutOverwriteCommand(5, roleId, "role", 1L, 0L));
		await handler.HandleAsync(new PutOverwriteCommand(5, roleId, "role", 4L, 8L));

		Assert.Single(overwrites.Store);
		var only = overwrites.Store.Values.Single();
		Assert.Equal(4L, only.Allow);
		Assert.Equal(8L, only.Deny);
	}

	[Fact]
	public async Task AllowDenyOverlap_Rejected()
	{
		var (handler, guilds, channels, _) = MakeHandler(currentUser: 1);
		channels.Seed(Channel.Create(5, 100, null, "g", null, ChannelType.Text, 0, Now).Value);
		var roleId = guilds.Store[100].Roles.First(r => r.IsDefault).Id;

		var result = await handler.HandleAsync(
			new PutOverwriteCommand(5, roleId, "role", 1L, 1L));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.OverwriteAllowDenyOverlap", result.Error.Code);
	}

	[Fact]
	public async Task MemberTarget_AcceptsUserKeyword()
	{
		// contract wire type for member-targeted overwrites is "user"; the parser
		// accepts both "user" and "member" and maps either to OverwriteTargetType.Member
		var (handler, guilds, channels, _) = MakeHandler(currentUser: 1);
		channels.Seed(Channel.Create(5, 100, null, "g", null, ChannelType.Text, 0, Now).Value);

		// owner (userId=1) is already a guild member -- use them as the target
		var result = await handler.HandleAsync(
			new PutOverwriteCommand(5, 1, "user", (long)Permission.SendMessages, 0L));

		Assert.True(result.Succeeded);
	}

	[Fact]
	public async Task UserWithManageChannels_CanPutOverwrite()
	{
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
		var result = await handler.HandleAsync(
			new PutOverwriteCommand(5, everyoneId, "role", (long)Permission.SendMessages, 0L));

		Assert.True(result.Succeeded);
		Assert.Single(overwrites.Store);
	}

	[Fact]
	public async Task UserWithOnlyManageRoles_CannotPutOverwrite()
	{
		// ManageRoles should NOT grant the ability to edit channel overwrites;
		// only ManageChannels (or Administrator) should
		var (handler, guilds, channels, _) = MakeHandler(currentUser: 2);
		channels.Seed(Channel.Create(5, 100, null, "g", null, ChannelType.Text, 0, Now).Value);

		var guild = guilds.Store[100];
		var manageRolesRole = DomainSeed.AddCustomRole(
			guild, roleId: 200, name: "RoleMod",
			permissions: (long)Permission.ManageRoles,
			position: 2, now: Now);
		DomainSeed.AddMember(guild, userId: 2, joinedAt: Now);
		DomainSeed.AssignRole(guild, userId: 2, roleId: manageRolesRole.Id, now: Now);

		var everyoneId = guild.Roles.First(r => r.IsDefault).Id;
		var result = await handler.HandleAsync(
			new PutOverwriteCommand(5, everyoneId, "role", (long)Permission.SendMessages, 0L));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.MissingPermission", result.Error.Code);
	}

	[Fact]
	public async Task SecondPut_AllowDenyOverlap_Rejected()
	{
		// first PUT creates the overwrite; second PUT with overlapping bits must
		// surface the validation failure from the update path (not just from create)
		var (handler, guilds, channels, _) = MakeHandler(currentUser: 1);
		channels.Seed(Channel.Create(5, 100, null, "g", null, ChannelType.Text, 0, Now).Value);
		var roleId = guilds.Store[100].Roles.First(r => r.IsDefault).Id;

		await handler.HandleAsync(new PutOverwriteCommand(5, roleId, "role", 1L, 0L));
		var result = await handler.HandleAsync(new PutOverwriteCommand(5, roleId, "role", 1L, 1L));

		Assert.True(result.IsFailure);
		Assert.Equal("Guild.OverwriteAllowDenyOverlap", result.Error.Code);
	}

	[Fact]
	public async Task MemberTarget_ValidMemberInMultiMemberGuild_Succeeds()
	{
		// ensures the member-existence check uses All(m => m.UserId != targetId):
		// Any(m => m.UserId != 2) is true in a two-member guild because member 1
		// satisfies != 2, which would incorrectly reject member 2 as a valid target
		var (handler, guilds, channels, overwrites) = MakeHandler(currentUser: 1);
		channels.Seed(Channel.Create(5, 100, null, "g", null, ChannelType.Text, 0, Now).Value);
		DomainSeed.AddMember(guilds.Store[100], userId: 2, joinedAt: Now);

		var result = await handler.HandleAsync(
			new PutOverwriteCommand(5, 2, "member", (long)Permission.SendMessages, 0L));

		Assert.True(result.Succeeded);
		Assert.Single(overwrites.Store);
	}

	private static (
		Guild.Application.Abstractions.Messaging.ICommandHandler<PutOverwriteCommand, Result> Handler,
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

		var handler = HandlerFactory.CreateCommand<PutOverwriteCommand, Result>(
			guilds, channels, overwrites,
			new FakeIdGenerator(), new FakeClock(), new FakeCurrentUser { Id = currentUser });
		return (handler, guilds, channels, overwrites);
	}
}
