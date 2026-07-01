using Guild.Application.Contracts;
using Guild.Application.Features.Channels.Permissions.DeleteOverwrite;
using Guild.Application.Features.Channels.Permissions.PutOverwrite;
using Guild.Application.Features.Membership.AssignRole;
using Guild.Application.Features.Membership.UnassignRole;
using Guild.Application.Features.Roles.Common;
using Guild.Application.Features.Roles.DeleteRole;
using Guild.Application.Features.Roles.UpdateRole;
using Guild.Domain.Guild;
using Guild.Domain.Results;
using Guild.UnitTests.Fakes;
using Xunit;
using GuildEntity = Guild.Domain.Guild.Guild;

namespace Guild.UnitTests.Application;

/// <summary>
/// covers channel.access_revoked emission: a revocation fires exactly when a
/// member could read a channel before a mutation but not after, and stays silent
/// when read access survives via another role or overwrite.
/// </summary>
public sealed class ChannelAccessRevokedTests
{
	private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
	private const long Read = (long)Permission.ReadMessages;
	private const long Owner = 1;
	private const long Member = 2;
	private const long ChannelId = 500;

	// builds guild 100 with owner 1, a channel, and member 2. everyonePerms sets
	// what the @everyone role grants (Read by default; 0 to force role/overwrite-only read).
	private static (FakeGuildRepository Guilds, FakeChannelRepository Channels, FakeChannelPermissionOverwriteRepository Overwrites, GuildEntity Guild)
		Setup(long everyonePerms = Read)
	{
		var guilds = new FakeGuildRepository();
		var guild = GuildEntity.Create(
			id: 100, name: "T", description: null, iconUrl: null, bannerUrl: null,
			ownerId: Owner, everyoneRoleId: 101, adminRoleId: 102, now: Now).Value;
		DomainSeed.AddMember(guild, Member, Now);
		DomainSeed.SetRolePermissions(guild.Roles.First(r => r.IsDefault), everyonePerms);
		guilds.Add(guild);

		var channels = new FakeChannelRepository();
		channels.Seed(Channel.Create(ChannelId, 100, null, "general", null, ChannelType.Text, 0, Now).Value);

		return (guilds, channels, new FakeChannelPermissionOverwriteRepository(), guild);
	}

	private static void SeedMemberOverwrite(FakeChannelPermissionOverwriteRepository ow, long userId, long allow, long deny) =>
		ow.Seed(ChannelPermissionOverwrite.Create(900 + userId, 100, ChannelId, OverwriteTargetType.Member, userId, allow, deny, Now).Value);

	[Fact]
	public async Task PutOverwrite_DenyReadToMember_EmitsRevocation()
	{
		var (guilds, channels, ow, _) = Setup(); // member reads via @everyone
		var bus = new FakeEventBus();
		var handler = HandlerFactory.CreateCommand<PutOverwriteCommand, Result>(
			guilds, channels, ow, bus, new FakeIdGenerator(), new FakeClock(), new FakeCurrentUser { Id = Owner });

		var result = await handler.HandleAsync(
			new PutOverwriteCommand(ChannelId, Member, "member", Allow: 0, Deny: Read));

		Assert.True(result.Succeeded);
		var evt = bus.Single<ChannelAccessRevoked>();
		Assert.Equal((ChannelId, Member), (evt.ChannelId, evt.UserId));
	}

	[Fact]
	public async Task DeleteOverwrite_ReadStillGrantedByEveryone_NoRevocation()
	{
		var (guilds, channels, ow, _) = Setup(); // @everyone grants read
		SeedMemberOverwrite(ow, Member, allow: Read, deny: 0); // redundant allow
		var bus = new FakeEventBus();
		var handler = HandlerFactory.CreateCommand<DeleteOverwriteCommand, Result>(
			guilds, channels, ow, bus, new FakeCurrentUser { Id = Owner });

		var result = await handler.HandleAsync(new DeleteOverwriteCommand(ChannelId, Member));

		Assert.True(result.Succeeded);
		Assert.Empty(bus.Published);
	}

	[Fact]
	public async Task DeleteOverwrite_SoleReadSourceRemoved_EmitsRevocation()
	{
		var (guilds, channels, ow, _) = Setup(everyonePerms: 0); // no base read
		SeedMemberOverwrite(ow, Member, allow: Read, deny: 0); // only read source
		var bus = new FakeEventBus();
		var handler = HandlerFactory.CreateCommand<DeleteOverwriteCommand, Result>(
			guilds, channels, ow, bus, new FakeCurrentUser { Id = Owner });

		var result = await handler.HandleAsync(new DeleteOverwriteCommand(ChannelId, Member));

		Assert.True(result.Succeeded);
		var evt = bus.Single<ChannelAccessRevoked>();
		Assert.Equal((ChannelId, Member), (evt.ChannelId, evt.UserId));
	}

	[Fact]
	public async Task AssignRole_WithDenyReadOverwrite_EmitsRevocation()
	{
		var (guilds, channels, ow, guild) = Setup(); // member reads via @everyone
		DomainSeed.AddCustomRole(guild, 200, "R", permissions: 0, position: 1, Now);
		// R denies read on the channel via an overwrite; assigning R applies that deny to the member
		ow.Seed(ChannelPermissionOverwrite.Create(800, 100, ChannelId, OverwriteTargetType.Role, 200, allow: 0, deny: Read, Now).Value);
		var bus = new FakeEventBus();
		var handler = HandlerFactory.CreateCommand<AssignRoleCommand, Result>(
			guilds, channels, ow, bus, new FakeClock(Now), new FakeCurrentUser { Id = Owner });

		var result = await handler.HandleAsync(new AssignRoleCommand(100, Member, 200));

		Assert.True(result.Succeeded);
		var evt = bus.Single<ChannelAccessRevoked>();
		Assert.Equal((ChannelId, Member), (evt.ChannelId, evt.UserId));
	}

	[Fact]
	public async Task AssignRole_DenyOverwrite_ButAllowOverwriteOnAnotherRoleWins_NoRevocation()
	{
		// Skaf holds "Astral Freak" (id 201) with an allow-read overwrite, sitting
		// BELOW the "Cone of shame" (id 200) deny-read overwrite being assigned.
		// at the role-overwrite tier allow wins regardless of position, so read survives.
		var (guilds, channels, ow, guild) = Setup(everyonePerms: 0);
		DomainSeed.AddCustomRole(guild, 201, "Astral Freak", permissions: 0, position: 1, Now);
		DomainSeed.AssignRole(guild, Member, 201, Now);
		ow.Seed(ChannelPermissionOverwrite.Create(801, 100, ChannelId, OverwriteTargetType.Role, 201, allow: Read, deny: 0, Now).Value);

		DomainSeed.AddCustomRole(guild, 200, "Cone of shame", permissions: 0, position: 2, Now);
		ow.Seed(ChannelPermissionOverwrite.Create(800, 100, ChannelId, OverwriteTargetType.Role, 200, allow: 0, deny: Read, Now).Value);

		var bus = new FakeEventBus();
		var handler = HandlerFactory.CreateCommand<AssignRoleCommand, Result>(
			guilds, channels, ow, bus, new FakeClock(Now), new FakeCurrentUser { Id = Owner });

		var result = await handler.HandleAsync(new AssignRoleCommand(100, Member, 200));

		Assert.True(result.Succeeded);
		Assert.Empty(bus.Published);
	}

	[Fact]
	public async Task UnassignRole_SoleReadRole_EmitsRevocation()
	{
		var (guilds, channels, ow, guild) = Setup(everyonePerms: 0);
		DomainSeed.AddCustomRole(guild, 200, "R", Read, 1, Now);
		DomainSeed.AssignRole(guild, Member, 200, Now);
		var bus = new FakeEventBus();
		var handler = HandlerFactory.CreateCommand<UnassignRoleCommand, Result>(
			guilds, channels, ow, bus, new FakeClock(Now), new FakeCurrentUser { Id = Owner });

		var result = await handler.HandleAsync(new UnassignRoleCommand(100, Member, 200));

		Assert.True(result.Succeeded);
		var evt = bus.Single<ChannelAccessRevoked>();
		Assert.Equal((ChannelId, Member), (evt.ChannelId, evt.UserId));
	}

	[Fact]
	public async Task UnassignRole_ReadRetainedBySecondRole_NoRevocation()
	{
		var (guilds, channels, ow, guild) = Setup(everyonePerms: 0);
		DomainSeed.AddCustomRole(guild, 200, "R1", Read, 1, Now);
		DomainSeed.AddCustomRole(guild, 201, "R2", Read, 2, Now);
		DomainSeed.AssignRole(guild, Member, 200, Now);
		DomainSeed.AssignRole(guild, Member, 201, Now);
		var bus = new FakeEventBus();
		var handler = HandlerFactory.CreateCommand<UnassignRoleCommand, Result>(
			guilds, channels, ow, bus, new FakeClock(Now), new FakeCurrentUser { Id = Owner });

		var result = await handler.HandleAsync(new UnassignRoleCommand(100, Member, 200));

		Assert.True(result.Succeeded);
		Assert.Empty(bus.Published);
	}

	[Fact]
	public async Task DeleteRole_SoleReadRole_EmitsRevocation()
	{
		var (guilds, channels, ow, guild) = Setup(everyonePerms: 0);
		DomainSeed.AddCustomRole(guild, 200, "R", Read, 1, Now);
		DomainSeed.AssignRole(guild, Member, 200, Now);
		var bus = new FakeEventBus();
		var handler = HandlerFactory.CreateCommand<DeleteRoleCommand, Result>(
			guilds, channels, ow, bus, new FakeClock(Now), new FakeCurrentUser { Id = Owner });

		var result = await handler.HandleAsync(new DeleteRoleCommand(100, 200));

		Assert.True(result.Succeeded);
		var evt = bus.Single<ChannelAccessRevoked>();
		Assert.Equal((ChannelId, Member), (evt.ChannelId, evt.UserId));
	}

	[Fact]
	public async Task UpdateRole_RemovesReadBit_EmitsRevocation()
	{
		var (guilds, channels, ow, guild) = Setup(everyonePerms: 0);
		DomainSeed.AddCustomRole(guild, 200, "R", Read, 1, Now);
		DomainSeed.AssignRole(guild, Member, 200, Now);
		var bus = new FakeEventBus();
		var handler = HandlerFactory.CreateCommand<UpdateRoleCommand, Result<RoleResponse>>(
			guilds, channels, ow, bus, new FakeClock(Now), new FakeCurrentUser { Id = Owner });

		var result = await handler.HandleAsync(new UpdateRoleCommand(
			100, 200, Name: null, Color: null, Permissions: 0, IsHoisted: null, IsMentionable: null));

		Assert.True(result.Succeeded);
		var evt = bus.Single<ChannelAccessRevoked>();
		Assert.Equal((ChannelId, Member), (evt.ChannelId, evt.UserId));
	}

	[Fact]
	public async Task UpdateRole_GrantsReadBit_NoRevocation()
	{
		// a grant can never revoke (base perms are additive), so no event
		var (guilds, channels, ow, guild) = Setup(everyonePerms: 0);
		DomainSeed.AddCustomRole(guild, 200, "R", permissions: 0, position: 1, Now);
		DomainSeed.AssignRole(guild, Member, 200, Now);
		var bus = new FakeEventBus();
		var handler = HandlerFactory.CreateCommand<UpdateRoleCommand, Result<RoleResponse>>(
			guilds, channels, ow, bus, new FakeClock(Now), new FakeCurrentUser { Id = Owner });

		var result = await handler.HandleAsync(new UpdateRoleCommand(
			100, 200, Name: null, Color: null, Permissions: Read, IsHoisted: null, IsMentionable: null));

		Assert.True(result.Succeeded);
		Assert.Empty(bus.Published);
	}

	[Fact]
	public async Task DeleteRole_NoHolders_NoRevocation()
	{
		var (guilds, channels, ow, guild) = Setup(everyonePerms: 0);
		DomainSeed.AddCustomRole(guild, 200, "R", permissions: Read, position: 1, Now); // nobody assigned
		var bus = new FakeEventBus();
		var handler = HandlerFactory.CreateCommand<DeleteRoleCommand, Result>(
			guilds, channels, ow, bus, new FakeClock(Now), new FakeCurrentUser { Id = Owner });

		var result = await handler.HandleAsync(new DeleteRoleCommand(100, 200));

		Assert.True(result.Succeeded);
		Assert.Empty(bus.Published);
	}
}
